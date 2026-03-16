using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Scripting;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Weapons;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Weapons;
using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Runs a complete combat simulation. Pure domain logic -- no grain dependencies.
/// Takes snapshots of ships, the combat grid, and script execution delegates.
/// Returns a CombatSimulationResult with the outcome and full event stream.
///
/// The simulation loop runs at a fixed tick rate, processing in this order each tick:
/// 1. Compute ticks -- execute scripts for eligible ships
/// 2. Physics -- update all ship positions
/// 3. Advance projectiles
/// 4. Environment -- gravity, collisions, heat, event horizon
/// 5. Weapon cooldowns decrement
/// 6. Auto-fire for ships not on compute tick
/// 7. Collision detection (projectile-ship)
/// 8. Damage resolution for hits
/// 9. Flee boundary checks
/// 10. Check combat end conditions
/// 11. Record events
/// </summary>
public class CombatSimulation
{
    private readonly CombatGrid _grid;
    private readonly List<CombatShipSnapshot> _attackerShips;
    private readonly List<CombatShipSnapshot> _defenderShips;
    private readonly Random _rng;
    private readonly SectorType _sectorType;
    private readonly ComputeTickScheduler _computeScheduler;
    private readonly WeaponCooldownTracker _cooldownTracker;
    private readonly List<CombatEvent> _events;
    private readonly List<DestroyedShipRecord> _destroyedShips;
    private readonly List<FledShipRecord> _fledShips;

    // Active projectiles currently in flight
    private readonly List<Projectile> _projectiles;

    // Script execution delegates -- called on compute ticks.
    // Return the list of ScriptCommands issued by the script during that tick.
    private readonly Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? _attackerOnTick;
    private readonly Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? _defenderOnTick;

    // Track which ship last damaged each ship (for destroyer attribution)
    private readonly Dictionary<Guid, Guid> _lastDamageSource;

    // Set of ship IDs on compute tick this sim tick (to skip auto-fire for them)
    private readonly HashSet<Guid> _shipsOnComputeThisTick;

    /// <summary>
    /// Tick rate: simulation ticks per second. Constant across all sector types.
    /// </summary>
    public const int TicksPerSecond = CombatConstants.SimTicksPerSecond;

    public CombatSimulation(
        CombatGrid grid,
        List<CombatShipSnapshot> attackerShips,
        List<CombatShipSnapshot> defenderShips,
        Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? attackerOnTick,
        Func<CombatShipSnapshot, CombatSimulationState, List<ScriptCommand>>? defenderOnTick,
        Random rng,
        SectorType sectorType)
    {
        _grid = grid;
        _attackerShips = attackerShips;
        _defenderShips = defenderShips;
        _attackerOnTick = attackerOnTick;
        _defenderOnTick = defenderOnTick;
        _rng = rng;
        _sectorType = sectorType;

        _computeScheduler = new ComputeTickScheduler();
        _cooldownTracker = new WeaponCooldownTracker();
        _events = new List<CombatEvent>();
        _destroyedShips = new List<DestroyedShipRecord>();
        _fledShips = new List<FledShipRecord>();
        _projectiles = new List<Projectile>();
        _lastDamageSource = new Dictionary<Guid, Guid>();
        _shipsOnComputeThisTick = new HashSet<Guid>();
    }

    /// <summary>
    /// Run the full combat simulation to completion.
    /// Returns a CombatSimulationResult with the outcome, events, and ship data.
    /// </summary>
    public CombatSimulationResult Run()
    {
        int maxTicks = GetMaxTicks(_sectorType);

        // Handle edge case: empty ship lists
        var (earlyEnd, earlyOutcome) = CombatEndConditionChecker.Check(
            _attackerShips, _defenderShips, 0, maxTicks);
        if (earlyEnd)
        {
            return BuildResult(earlyOutcome!.Value, 0, maxTicks);
        }

        // Initialize compute tick scheduler with all ships
        var allShips = new List<CombatShipSnapshot>();
        allShips.AddRange(_attackerShips);
        allShips.AddRange(_defenderShips);
        _computeScheduler.Initialize(allShips);

        // Record combat start event
        _events.Add(new CombatEvent
        {
            Tick = 0,
            EventType = "CombatStarted",
            Detail = $"Attackers:{_attackerShips.Count} Defenders:{_defenderShips.Count} Sector:{_sectorType}"
        });

        // Main simulation loop
        for (int tick = 0; tick < maxTicks; tick++)
        {
            // --- Step 1: Compute ticks ---
            ExecuteComputeTicks(tick);

            // --- Step 2: Physics -- update all ship positions ---
            UpdateAllShipPositions();

            // --- Step 2b: Record ship positions for replay ---
            RecordShipPositions(tick);

            // --- Step 3: Advance projectiles ---
            AdvanceProjectiles();

            // --- Step 4: Environment -- gravity, collisions, heat, event horizon ---
            ProcessEnvironmentForAllShips(tick);

            // --- Step 5: Weapon cooldowns decrement ---
            _cooldownTracker.DecrementAll();

            // --- Step 6: Auto-fire for ships NOT on compute tick ---
            ExecuteAutoFire(tick);

            // --- Step 7 & 8: Collision detection + damage resolution ---
            ResolveProjectileHits(tick);

            // --- Step 9: Flee boundary checks ---
            CheckFleeBoundaries(tick);

            // --- Step 10: Check combat end conditions ---
            var (ended, outcome) = CombatEndConditionChecker.Check(
                _attackerShips, _defenderShips, tick + 1, maxTicks);

            if (ended)
            {
                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "CombatEnded",
                    Detail = $"Outcome:{outcome}"
                });
                return BuildResult(outcome!.Value, tick + 1, maxTicks);
            }

            _shipsOnComputeThisTick.Clear();
        }

        // Should not reach here -- CombatEndConditionChecker handles timeout at maxTicks
        return BuildResult(CombatOutcome.TimedOut, maxTicks, maxTicks);
    }

    /// <summary>
    /// Execute compute ticks for eligible ships. Ships on compute tick run their scripts.
    /// </summary>
    private void ExecuteComputeTicks(int tick)
    {
        var eligibleShipIds = _computeScheduler.GetShipsForComputeTick(tick);
        _shipsOnComputeThisTick.Clear();

        foreach (var shipId in eligibleShipIds)
        {
            var ship = FindShip(shipId);
            if (ship == null || !ship.IsAlive || ship.HasFled) continue;

            _shipsOnComputeThisTick.Add(shipId);

            var state = BuildSimulationState(ship, tick);
            bool isAttacker = _attackerShips.Any(s => s.ShipId == shipId);

            try
            {
                var onTick = isAttacker ? _attackerOnTick : _defenderOnTick;
                var commands = onTick?.Invoke(ship, state) ?? new List<ScriptCommand>();
                ApplyScriptCommands(ship, isAttacker, commands, tick);
            }
            catch (Exception ex)
            {
                // Script error -- ship continues on current trajectory, auto-fire handles weapons.
                // Record the error so it surfaces in replay/diagnostics.
                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "ScriptError",
                    ShipId = shipId,
                    Position = ship.Position,
                    Detail = $"Side:{(isAttacker ? "Attacker" : "Defender")} Error:{ex.Message}"
                });
            }
        }
    }

    /// <summary>
    /// Apply script commands to the ship. Called after each compute tick.
    /// Commands are processed in order; HoldFire suppresses Fire/FireAt for the tick.
    /// </summary>
    private void ApplyScriptCommands(CombatShipSnapshot ship, bool isAttacker, List<ScriptCommand> commands, int tick)
    {
        if (commands.Count == 0) return;

        var enemies = isAttacker
            ? _defenderShips.Where(s => s.IsAlive && !s.HasFled).ToList()
            : _attackerShips.Where(s => s.IsAlive && !s.HasFled).ToList();

        bool holdFire = commands.Any(c => c.Type == ScriptCommandType.HoldFire);

        foreach (var cmd in commands)
        {
            switch (cmd.Type)
            {
                case ScriptCommandType.Thrust:
                    ShipPhysics.ApplyThrust(ship, cmd.Angle, cmd.Power);
                    break;

                case ScriptCommandType.MoveTo:
                    ShipPhysics.MoveToWaypoint(ship, new Vector2D(cmd.X, cmd.Y), cmd.Speed);
                    break;

                case ScriptCommandType.Stop:
                    ShipPhysics.StopThrust(ship);
                    break;

                case ScriptCommandType.Flee:
                    FleeBehavior.ExecuteFlee(ship);
                    break;

                case ScriptCommandType.HoldFire:
                    // Already handled above — no additional action needed.
                    break;

                case ScriptCommandType.Fire:
                    if (holdFire) break;
                    if (!Guid.TryParse(cmd.WeaponId, out var fireWeaponGuid)) break;
                    var fireWeapon = ship.Weapons.FirstOrDefault(w => w.ModuleId == fireWeaponGuid);
                    if (fireWeapon == null || fireWeapon.Condition <= 0) break;
                    if (!_cooldownTracker.IsReady(fireWeapon.ModuleId)) break;
                    if (!Guid.TryParse(cmd.TargetId, out var targetGuid)) break;
                    var fireTarget = enemies.FirstOrDefault(e => e.ShipId == targetGuid);
                    if (fireTarget == null) break;
                    FireWeapon(ship, fireWeapon, fireTarget, tick);
                    break;

                case ScriptCommandType.FireAt:
                    if (holdFire) break;
                    if (!Guid.TryParse(cmd.WeaponId, out var fireAtWeaponGuid)) break;
                    var fireAtWeapon = ship.Weapons.FirstOrDefault(w => w.ModuleId == fireAtWeaponGuid);
                    if (fireAtWeapon == null || fireAtWeapon.Condition <= 0) break;
                    if (!_cooldownTracker.IsReady(fireAtWeapon.ModuleId)) break;
                    // Fire at coordinates: find nearest enemy to the target position
                    var aimPos = new Vector2D(cmd.X, cmd.Y);
                    var nearestEnemy = enemies
                        .OrderBy(e => (e.Position.X - aimPos.X) * (e.Position.X - aimPos.X) +
                                      (e.Position.Y - aimPos.Y) * (e.Position.Y - aimPos.Y))
                        .FirstOrDefault();
                    if (nearestEnemy == null) break;
                    FireWeapon(ship, fireAtWeapon, nearestEnemy, tick);
                    break;
            }
        }
    }

    /// <summary>
    /// Update position for all living, non-fled ships.
    /// </summary>
    private void UpdateAllShipPositions()
    {
        foreach (var ship in GetAllLivingShips())
        {
            ShipPhysics.UpdatePosition(ship);
        }
    }

    /// <summary>
    /// Record position events for all living ships.
    /// Per spec: record on compute ticks, significant velocity changes, damage, or collisions.
    /// For simplicity, record every 5 ticks for all ships to keep replay data manageable.
    /// </summary>
    private void RecordShipPositions(int tick)
    {
        // Record every 5 ticks, or on tick 0 (initial positions)
        if (tick % 5 != 0 && tick != 0) return;

        foreach (var ship in GetAllLivingShips())
        {
            _events.Add(new CombatEvent
            {
                Tick = tick,
                EventType = "ShipMoved",
                ShipId = ship.ShipId,
                Position = ship.Position,
                Detail = $"VelX:{ship.Velocity.X:F2},VelY:{ship.Velocity.Y:F2}"
            });
        }
    }

    /// <summary>
    /// Advance all active kinetic projectiles and remove expired ones.
    /// </summary>
    private void AdvanceProjectiles()
    {
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var proj = _projectiles[i];

            if (!proj.IsActive)
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            proj.Advance();

            if (proj.IsOutOfBounds(_grid))
            {
                proj.Deactivate();
                _projectiles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Process environmental hazards for all living ships.
    /// </summary>
    private void ProcessEnvironmentForAllShips(int tick)
    {
        foreach (var ship in GetAllLivingShips().ToList())
        {
            var envEvents = EnvironmentProcessor.ProcessEnvironment(ship, _grid);
            foreach (var envEvent in envEvents)
            {
                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "EnvironmentalDamage",
                    ShipId = envEvent.ShipId,
                    Position = envEvent.ShipPosition,
                    DamageDealt = envEvent.DamageDealt,
                    Detail = envEvent.SourceType
                });

                if (envEvent.ShipDestroyed)
                {
                    RecordShipDestroyed(ship, null, tick);
                }
            }
        }
    }

    /// <summary>
    /// Execute auto-fire for ships that did NOT receive a compute tick this sim tick.
    /// Auto-fire targets the nearest enemy in range with any ready weapon.
    /// </summary>
    private void ExecuteAutoFire(int tick)
    {
        foreach (var ship in GetAllLivingShips().ToList())
        {
            if (_shipsOnComputeThisTick.Contains(ship.ShipId)) continue;

            bool isAttacker = _attackerShips.Any(s => s.ShipId == ship.ShipId);
            var enemies = isAttacker
                ? _defenderShips.Where(s => s.IsAlive && !s.HasFled).ToList()
                : _attackerShips.Where(s => s.IsAlive && !s.HasFled).ToList();

            if (enemies.Count == 0) continue;

            var target = AutoFireBehavior.SelectAutoFireTarget(ship, enemies);
            if (target == null) continue;

            foreach (var weapon in ship.Weapons)
            {
                if (!_cooldownTracker.IsReady(weapon.ModuleId)) continue;
                if (weapon.Condition <= 0) continue;

                FireWeapon(ship, weapon, target, tick);
            }
        }
    }

    /// <summary>
    /// Fire a weapon from source ship at target ship.
    /// Energy weapons resolve instantly (hitscan). Kinetic weapons create projectiles.
    /// </summary>
    private void FireWeapon(
        CombatShipSnapshot source,
        WeaponSnapshot weapon,
        CombatShipSnapshot target,
        int tick)
    {
        bool isNebula = _sectorType == SectorType.Nebula;
        bool sourceInDensePatch = isNebula && _grid.IsInNebulaPatch(source.Position);

        _events.Add(new CombatEvent
        {
            Tick = tick,
            EventType = "WeaponFired",
            ShipId = source.ShipId,
            TargetShipId = target.ShipId,
            Position = source.Position,
            Detail = $"{weapon.DamageType}:{weapon.BaseDamage}"
        });

        if (weapon.DamageType == DamageType.Energy)
        {
            var result = EnergyWeaponResolver.Fire(
                weapon, source, target, _grid, isNebula, sourceInDensePatch, _cooldownTracker, _rng);

            if (result.Hit && result.DamageResult != null)
            {
                DamageApplicator.ApplyDamageToShip(target, result.DamageResult);
                UpdateComputeSchedulerAfterDamage(target);
                _lastDamageSource[target.ShipId] = source.ShipId;

                var energyDetail = $"Type:{weapon.DamageType} Shield:{result.DamageResult.ShieldAbsorbed:F1} Armor:{result.DamageResult.ArmorAblated:F1} Structure:{result.DamageResult.StructureDamage:F1}";
                if (result.DamageResult.ModuleHit != null)
                {
                    energyDetail += $" ModuleHit:{result.DamageResult.ModuleHit.ModuleName} ModuleCondition:{result.DamageResult.ModuleHit.ConditionAfter:F1}";
                }

                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "DamageDealt",
                    ShipId = source.ShipId,
                    TargetShipId = target.ShipId,
                    Position = target.Position,
                    DamageDealt = result.DamageResult.StructureDamage,
                    // CAUTION: CombatInstanceRoot.ParseDamageDetail depends on this exact format.
                    // If you change the field names, order, or separators here, update
                    // ParseDamageDetail in CombatInstanceRoot.cs to match.
                    Detail = energyDetail
                });

                if (result.DamageResult.ModuleHit != null)
                {
                    if (result.DamageResult.ModuleHit.WasDestroyed)
                    {
                        _events.Add(new CombatEvent
                        {
                            Tick = tick,
                            EventType = "ModuleDestroyed",
                            ShipId = target.ShipId,
                            Detail = result.DamageResult.ModuleHit.ModuleName
                        });
                    }
                    else
                    {
                        _events.Add(new CombatEvent
                        {
                            Tick = tick,
                            EventType = "ModuleConditionChanged",
                            ShipId = target.ShipId,
                            Detail = $"Module:{result.DamageResult.ModuleHit.ModuleName} Condition:{result.DamageResult.ModuleHit.ConditionAfter:F1} ModuleId:{result.DamageResult.ModuleHit.ModuleId}"
                        });
                    }
                }

                if (!target.IsAlive)
                {
                    RecordShipDestroyed(target, source.ShipId, tick);
                }
            }
        }
        else if (weapon.DamageType == DamageType.Kinetic)
        {
            var result = KineticWeaponResolver.Fire(
                weapon, source, target, isNebula, sourceInDensePatch, _cooldownTracker, _rng);

            if (result.Created && result.Projectile != null)
            {
                _projectiles.Add(result.Projectile);
            }
        }

        // Set weapon on cooldown
        _cooldownTracker.SetCooldown(weapon.ModuleId, weapon.CooldownTicks);
    }

    /// <summary>
    /// Resolve projectile-ship collisions for all active projectiles.
    /// </summary>
    private void ResolveProjectileHits(int tick)
    {
        var allShips = GetAllLivingShips().ToList();

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var proj = _projectiles[i];
            if (!proj.IsActive) continue;

            // Use sweep collision to handle fast projectiles
            var previousPos = new Vector2D(
                proj.Position.X - proj.Velocity.X,
                proj.Position.Y - proj.Velocity.Y);

            var collisionResult = ProjectileCollisionResolver.CheckCollisionsSweep(
                proj, previousPos, allShips, _grid);

            if (collisionResult.HitShip != null)
            {
                var damageResult = DamagePipeline.Resolve(
                    proj.Damage, DamageType.Kinetic, collisionResult.HitShip, _rng);
                DamageApplicator.ApplyDamageToShip(collisionResult.HitShip, damageResult);
                UpdateComputeSchedulerAfterDamage(collisionResult.HitShip);

                _lastDamageSource[collisionResult.HitShip.ShipId] = proj.SourceShipId;

                var kineticDetail = $"Type:Kinetic Shield:{damageResult.ShieldAbsorbed:F1} Armor:{damageResult.ArmorAblated:F1} Structure:{damageResult.StructureDamage:F1}";
                if (damageResult.ModuleHit != null)
                {
                    kineticDetail += $" ModuleHit:{damageResult.ModuleHit.ModuleName} ModuleCondition:{damageResult.ModuleHit.ConditionAfter:F1}";
                }

                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "ProjectileHit",
                    ShipId = proj.SourceShipId,
                    TargetShipId = collisionResult.HitShip.ShipId,
                    Position = proj.Position,
                    DamageDealt = damageResult.StructureDamage,
                    // CAUTION: CombatSimulateCommand.RecordEvents parses this exact format.
                    // If you change the field names, order, or separators here, update
                    // RecordEvents in CombatSimulateCommand.cs to match.
                    Detail = kineticDetail
                });

                if (damageResult.ModuleHit != null)
                {
                    if (damageResult.ModuleHit.WasDestroyed)
                    {
                        _events.Add(new CombatEvent
                        {
                            Tick = tick,
                            EventType = "ModuleDestroyed",
                            ShipId = collisionResult.HitShip.ShipId,
                            Detail = damageResult.ModuleHit.ModuleName
                        });
                    }
                    else
                    {
                        _events.Add(new CombatEvent
                        {
                            Tick = tick,
                            EventType = "ModuleConditionChanged",
                            ShipId = collisionResult.HitShip.ShipId,
                            Detail = $"Module:{damageResult.ModuleHit.ModuleName} Condition:{damageResult.ModuleHit.ConditionAfter:F1} ModuleId:{damageResult.ModuleHit.ModuleId}"
                        });
                    }
                }

                if (!collisionResult.HitShip.IsAlive)
                {
                    RecordShipDestroyed(collisionResult.HitShip, proj.SourceShipId, tick);
                }

                proj.Deactivate();
                _projectiles.RemoveAt(i);
            }
            else if (collisionResult.DestroyedByObstacle)
            {
                proj.Deactivate();
                _projectiles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Check all ships for flee boundary crossing.
    /// </summary>
    private void CheckFleeBoundaries(int tick)
    {
        foreach (var ship in GetAllLivingShips().ToList())
        {
            if (FleeBehavior.CheckFled(ship, _grid))
            {
                _fledShips.Add(new FledShipRecord
                {
                    ShipId = ship.ShipId,
                    ExitPosition = ship.Position,
                    ExitTick = tick
                });

                _events.Add(new CombatEvent
                {
                    Tick = tick,
                    EventType = "ShipFled",
                    ShipId = ship.ShipId,
                    Position = ship.Position
                });
            }
        }
    }

    /// <summary>
    /// Record a ship destruction.
    /// </summary>
    private void RecordShipDestroyed(CombatShipSnapshot ship, Guid? destroyerId, int tick)
    {
        if (_destroyedShips.Any(d => d.ShipId == ship.ShipId)) return;

        _destroyedShips.Add(new DestroyedShipRecord
        {
            ShipId = ship.ShipId,
            DestroyerShipId = destroyerId,
            LastPosition = ship.Position,
            DestroyedAtTick = tick
        });

        _events.Add(new CombatEvent
        {
            Tick = tick,
            EventType = "ShipDestroyed",
            ShipId = ship.ShipId,
            Position = ship.Position,
            Detail = destroyerId.HasValue ? $"DestroyedBy:{destroyerId}" : "DestroyedBy:Environment"
        });
    }

    /// <summary>
    /// Updates the compute tick interval for a ship after it takes damage.
    /// This ensures compute-module damage degrades script execution frequency (death-spiral).
    /// </summary>
    private void UpdateComputeSchedulerAfterDamage(CombatShipSnapshot ship)
    {
        double effectiveCompute = DamageApplicator.CalculateEffectiveCompute(ship);
        _computeScheduler.UpdateShipInterval(ship.ShipId, effectiveCompute);
    }

    private CombatShipSnapshot? FindShip(Guid shipId)
    {
        return _attackerShips.FirstOrDefault(s => s.ShipId == shipId)
            ?? _defenderShips.FirstOrDefault(s => s.ShipId == shipId);
    }

    private IEnumerable<CombatShipSnapshot> GetAllLivingShips()
    {
        return _attackerShips.Where(s => s.IsAlive && !s.HasFled)
            .Concat(_defenderShips.Where(s => s.IsAlive && !s.HasFled));
    }

    private CombatSimulationState BuildSimulationState(CombatShipSnapshot ship, int tick)
    {
        bool isAttacker = _attackerShips.Any(s => s.ShipId == ship.ShipId);
        var friendlies = isAttacker ? _attackerShips : _defenderShips;
        var enemies = isAttacker ? _defenderShips : _attackerShips;

        return new CombatSimulationState
        {
            MyShip = ship,
            MyFleet = friendlies.Where(s => s.IsAlive && !s.HasFled).ToList(),
            EnemyFleet = enemies.Where(s => s.IsAlive && !s.HasFled).ToList(),
            Grid = _grid,
            Tick = tick,
            Projectiles = _projectiles.Where(p => p.IsActive).ToList()
        };
    }

    /// <summary>
    /// Get the maximum tick count for the sector type.
    /// Uses SectorCombatConfig for consistency with Phase 5 definitions.
    /// </summary>
    public static int GetMaxTicks(SectorType sectorType)
    {
        var sectorName = sectorType.ToString();
        try
        {
            var config = SectorCombatConfig.GetConfig(sectorName);
            return config.MaxTotalTicks;
        }
        catch
        {
            // Fallback for Nexus or unknown types
            return 3000;
        }
    }

    private CombatSimulationResult BuildResult(CombatOutcome outcome, int totalTicks, int maxTicks)
    {
        return new CombatSimulationResult
        {
            Outcome = outcome,
            TotalTicks = totalTicks,
            DurationSeconds = (double)totalTicks / TicksPerSecond,
            SurvivingAttackerShips = _attackerShips
                .Where(s => s.IsAlive && !s.HasFled).ToList(),
            SurvivingDefenderShips = _defenderShips
                .Where(s => s.IsAlive && !s.HasFled).ToList(),
            DestroyedShips = _destroyedShips,
            FledShips = _fledShips,
            Events = _events
        };
    }
}

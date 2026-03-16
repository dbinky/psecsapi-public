using psecsapi.Combat;
using psecsapi.Combat.Snapshots;
using psecsapi.Domain.Combat;
using psecsapi.Domain.Modules;

namespace psecsapi.Console.Commands.Combat.Simulation;

/// <summary>
/// Converts ShipConfig (JSON-friendly preset data) into CombatShipSnapshot
/// instances suitable for local combat simulation. Uses hardcoded lookup tables
/// for chassis stats and module capabilities derived from the tech tree.
/// </summary>
// Last synced with tech tree commit: 434c88639e34c35d29a23b3304169374611ce72b
public static class ShipSnapshotFactory
{
    // === Chassis Lookup ===
    // (structurePoints, hullPoints, baseMass)
    // Source: tech tree chassis definitions across T1-T7
    private static readonly Dictionary<string, (decimal Structure, decimal Hull, double Mass)> ChassisStats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // T1-T2: Light frames
            ["Shuttle"]     = (200m,  100m,  500.0),   // T1 starter
            ["Corvette"]    = (400m,  200m,  800.0),   // T2 fast scout
            // T3: Medium frames
            ["Frigate"]     = (800m,  400m,  1500.0),  // T3 versatile
            ["Destroyer"]   = (1200m, 600m,  2000.0),  // T3 combat-focused
            // T4-T5: Heavy frames
            ["Cruiser"]     = (2000m, 1000m, 3500.0),  // T4 mainline
            ["Battlecruiser"] = (3000m, 1500m, 5000.0),// T5 heavy combat
            // T6-T7: Capital frames
            ["Battleship"]  = (5000m, 2500m, 8000.0),  // T6 capital
            ["Dreadnought"] = (8000m, 4000m, 12000.0), // T7 super-capital
        };

    // === Module Stats Lookup ===
    // Maps module type name to (capabilityType, baseValue, powerRequired, isExterior, moduleMass)
    // Source: tech tree module definitions
    private static readonly Dictionary<string, ModuleStats> ModuleStatsLookup =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Engines (Interior) — Speed capability
            ["IonDrive"]        = new(ModuleCapabilityType.Speed, 30.0m,  5.0, false, 100.0),   // T2
            ["FusionDrive"]     = new(ModuleCapabilityType.Speed, 50.0m,  8.0, false, 150.0),   // T3
            ["PlasmaDrive"]     = new(ModuleCapabilityType.Speed, 70.0m,  12.0, false, 200.0),  // T4
            ["WarpDrive"]       = new(ModuleCapabilityType.Speed, 100.0m, 18.0, false, 300.0),  // T5
            ["DarkMatterDrive"] = new(ModuleCapabilityType.Speed, 140.0m, 25.0, false, 400.0),  // T6
            ["QuantumDrive"]    = new(ModuleCapabilityType.Speed, 200.0m, 35.0, false, 500.0),  // T7

            // Shields (Exterior) — EnergyResistance capability
            ["PhaseShield"]     = new(ModuleCapabilityType.EnergyResistance, 20.0m, 8.0, true, 80.0),   // T3
            ["PlasmaShield"]    = new(ModuleCapabilityType.EnergyResistance, 35.0m, 12.0, true, 120.0), // T4
            ["HardlightShield"] = new(ModuleCapabilityType.EnergyResistance, 50.0m, 18.0, true, 160.0), // T5
            ["VoidShield"]      = new(ModuleCapabilityType.EnergyResistance, 70.0m, 25.0, true, 220.0), // T6
            ["NovaShield"]      = new(ModuleCapabilityType.EnergyResistance, 100.0m, 35.0, true, 300.0),// T7

            // Armor (Exterior) — KineticResistance capability
            ["ReactiveArmor"]   = new(ModuleCapabilityType.KineticResistance, 20.0m, 2.0, true, 150.0),  // T3
            ["CompositeArmor"]  = new(ModuleCapabilityType.KineticResistance, 35.0m, 3.0, true, 250.0),  // T4
            ["NanoweaveArmor"]  = new(ModuleCapabilityType.KineticResistance, 50.0m, 4.0, true, 350.0),  // T5
            ["AblativeArmor"]   = new(ModuleCapabilityType.KineticResistance, 70.0m, 5.0, true, 500.0),  // T6
            ["NovaPlating"]     = new(ModuleCapabilityType.KineticResistance, 100.0m, 6.0, true, 700.0), // T7

            // Reactors (Interior) — PowerGeneration capability
            ["FissionReactor"]  = new(ModuleCapabilityType.PowerGeneration, 20.0m, 0.0, false, 200.0),  // T2
            ["FusionReactor"]   = new(ModuleCapabilityType.PowerGeneration, 40.0m, 0.0, false, 300.0),  // T3
            ["PlasmaReactor"]   = new(ModuleCapabilityType.PowerGeneration, 60.0m, 0.0, false, 400.0),  // T4
            ["AntimatterReactor"] = new(ModuleCapabilityType.PowerGeneration, 90.0m, 0.0, false, 500.0),// T5
            ["DarkMatterReactor"] = new(ModuleCapabilityType.PowerGeneration, 130.0m, 0.0, false, 600.0),// T6
            ["NovaReactor"]     = new(ModuleCapabilityType.PowerGeneration, 180.0m, 0.0, false, 700.0), // T7

            // Compute (Interior) — ComputeCapacity capability
            ["BasicComputer"]   = new(ModuleCapabilityType.ComputeCapacity, 10.0m, 3.0, false, 50.0),   // T1
            ["TacticalComputer"]= new(ModuleCapabilityType.ComputeCapacity, 20.0m, 5.0, false, 80.0),   // T3
            ["QuantumComputer"] = new(ModuleCapabilityType.ComputeCapacity, 40.0m, 8.0, false, 120.0),  // T5
            ["NovaComputer"]    = new(ModuleCapabilityType.ComputeCapacity, 60.0m, 12.0, false, 160.0), // T7

            // Sensors (Interior) — Sensors capability
            ["BasicSensors"]    = new(ModuleCapabilityType.Sensors, 10.0m, 2.0, false, 30.0),   // T1
            ["LongRangeSensors"]= new(ModuleCapabilityType.Sensors, 25.0m, 4.0, false, 60.0),   // T3
            ["DeepScanArray"]   = new(ModuleCapabilityType.Sensors, 50.0m, 8.0, false, 100.0),  // T5

            // Targeting (Interior) — WeaponRange capability
            ["TargetingArray"]  = new(ModuleCapabilityType.WeaponRange, 500.0m, 3.0, false, 40.0),  // T3
            ["AdvancedTargeting"]= new(ModuleCapabilityType.WeaponRange, 1000.0m, 5.0, false, 70.0),// T5
        };

    // === Weapon Stats Lookup ===
    // Maps weapon name to (damageType, baseDamage, weaponMass)
    // Cooldowns and projectile speeds come from WeaponCooldownMapper and ProjectileSpeedMapper
    private static readonly Dictionary<string, WeaponStats> WeaponStatsLookup =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Energy weapons (T3-T7) — hitscan, EnergyDamage
            ["PulseLaser"]      = new(DamageType.Energy, 15.0, 60.0),    // T3
            ["BeamCannon"]      = new(DamageType.Energy, 25.0, 90.0),    // T4
            ["PlasmaLance"]     = new(DamageType.Energy, 40.0, 130.0),   // T5
            ["DisruptorArray"]  = new(DamageType.Energy, 60.0, 180.0),   // T6
            ["NovaProjector"]   = new(DamageType.Energy, 90.0, 250.0),   // T7

            // Kinetic weapons (T2-T7) — projectile travel, KineticDamage
            ["Autocannon"]      = new(DamageType.Kinetic, 8.0, 40.0),    // T2
            ["MassDriver"]      = new(DamageType.Kinetic, 18.0, 80.0),   // T3
            ["Railgun"]         = new(DamageType.Kinetic, 35.0, 120.0),  // T4
            ["GaussLance"]      = new(DamageType.Kinetic, 55.0, 170.0),  // T5
            ["CoilgunArray"]    = new(DamageType.Kinetic, 80.0, 230.0),  // T6
            ["NovaDriver"]      = new(DamageType.Kinetic, 120.0, 320.0), // T7
        };

    /// <summary>
    /// Converts a ShipConfig into a CombatShipSnapshot for local simulation.
    /// Generates deterministic IDs based on ship name and index.
    /// </summary>
    public static CombatShipSnapshot CreateSnapshot(ShipConfig shipConfig, Guid corpId, Guid fleetId, int shipIndex)
    {
        // Generate deterministic IDs from ship name + index
        var shipId = GenerateDeterministicGuid($"ship-{shipIndex}-{shipConfig.Name}");

        // Resolve chassis stats
        if (!ChassisStats.TryGetValue(shipConfig.Chassis, out var chassis))
            throw new ArgumentException(
                $"Unknown chassis '{shipConfig.Chassis}'. Available: {string.Join(", ", ChassisStats.Keys.OrderBy(k => k))}");

        // Build module snapshots and accumulate stats
        var modules = new List<ModuleSnapshot>();
        double totalSpeed = 0;
        double totalComputeCapacity = 0;
        double totalEnergyResistance = 0;
        double totalKineticResistance = 0;
        double totalPowerGeneration = 0;
        double totalPowerRequired = 0;
        double totalSensorCapability = 0;
        double totalWeaponRange = 0;
        double totalModuleMass = 0;

        int moduleIndex = 0;
        foreach (var mod in shipConfig.Modules)
        {
            if (!ModuleStatsLookup.TryGetValue(mod.Type, out var stats))
                throw new ArgumentException(
                    $"Unknown module type '{mod.Type}'. Available: {string.Join(", ", ModuleStatsLookup.Keys.OrderBy(k => k))}");

            var moduleId = GenerateDeterministicGuid($"module-{shipIndex}-{moduleIndex}-{mod.Type}");
            bool isExterior = stats.IsExterior || mod.Slot.Equals("Exterior", StringComparison.OrdinalIgnoreCase);

            modules.Add(new ModuleSnapshot
            {
                ModuleId = moduleId,
                Name = mod.Type,
                IsExterior = isExterior,
                Condition = 100m,
                Capabilities = new List<ModuleCapabilitySnapshot>
                {
                    new() { CapabilityType = stats.CapabilityType, BaseValue = stats.BaseValue }
                },
                PowerRequired = stats.PowerRequired
            });

            totalModuleMass += stats.ModuleMass;
            totalPowerRequired += stats.PowerRequired;

            switch (stats.CapabilityType)
            {
                case ModuleCapabilityType.Speed:
                    totalSpeed += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.ComputeCapacity:
                    totalComputeCapacity += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.EnergyResistance:
                    totalEnergyResistance += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.KineticResistance:
                    totalKineticResistance += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.PowerGeneration:
                    totalPowerGeneration += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.Sensors:
                    totalSensorCapability += (double)stats.BaseValue;
                    break;
                case ModuleCapabilityType.WeaponRange:
                    totalWeaponRange += (double)stats.BaseValue;
                    break;
            }

            moduleIndex++;
        }

        // Build weapon snapshots
        var weapons = new List<WeaponSnapshot>();
        int weaponIndex = 0;
        foreach (var weaponName in shipConfig.Weapons)
        {
            if (!WeaponStatsLookup.TryGetValue(weaponName, out var wStats))
                throw new ArgumentException(
                    $"Unknown weapon '{weaponName}'. Available: {string.Join(", ", WeaponStatsLookup.Keys.OrderBy(k => k))}");

            var weaponId = GenerateDeterministicGuid($"weapon-{shipIndex}-{weaponIndex}-{weaponName}");
            double effectiveRange = CombatConstants.BaseWeaponRange + totalWeaponRange;

            // Also add weapon as a module snapshot (weapons occupy exterior slots)
            modules.Add(new ModuleSnapshot
            {
                ModuleId = weaponId,
                Name = weaponName,
                IsExterior = true,
                Condition = 100m,
                Capabilities = new List<ModuleCapabilitySnapshot>
                {
                    new()
                    {
                        CapabilityType = wStats.DamageType == DamageType.Energy
                            ? ModuleCapabilityType.EnergyDamage
                            : ModuleCapabilityType.KineticDamage,
                        BaseValue = (decimal)wStats.BaseDamage
                    }
                },
                PowerRequired = 5.0 // Standard weapon power draw
            });

            totalModuleMass += wStats.WeaponMass;
            totalPowerRequired += 5.0;

            weapons.Add(new WeaponSnapshot
            {
                ModuleId = weaponId,
                DamageType = wStats.DamageType,
                BaseDamage = wStats.BaseDamage,
                CooldownTicks = WeaponCooldownMapper.GetCooldownTicks(weaponName),
                ProjectileSpeed = ProjectileSpeedMapper.GetProjectileSpeed(weaponName),
                Range = effectiveRange,
                ConeAngle = CombatConstants.BaseConeAngle, // Default cone, no sensitivity modifier
                Condition = 100m
            });

            weaponIndex++;
        }

        double totalMass = chassis.Mass + totalModuleMass;
        double maxSpeed = totalSpeed;
        // Acceleration scaled so ships reach max speed in ~100 ticks (10 seconds).
        // Heavier ships accelerate slightly slower via a mass dampening factor.
        double massFactor = 1000.0 / (totalMass + 1000.0); // 0.5 for 1000kg, 0.33 for 2000kg, etc.
        double maxAcceleration = totalSpeed > 0 ? (totalSpeed / 100.0) * massFactor : 0;

        return new CombatShipSnapshot
        {
            ShipId = shipId,
            ShipName = shipConfig.Name,
            CorpId = corpId,
            FleetId = fleetId,
            Position = Vector2D.Zero,
            Velocity = Vector2D.Zero,
            Facing = 0,
            CurrentSpeed = 0,
            MaxSpeed = maxSpeed,
            MaxAcceleration = maxAcceleration,
            Mass = totalMass,
            ComputeCapacity = totalComputeCapacity,
            CurrentStructurePoints = chassis.Structure,
            MaxStructurePoints = chassis.Structure,
            CurrentHullPoints = chassis.Hull,
            MaxHullPoints = chassis.Hull,
            Weapons = weapons,
            Modules = modules,
            TotalEnergyResistance = totalEnergyResistance,
            TotalKineticResistance = totalKineticResistance,
            TotalPowerGeneration = totalPowerGeneration,
            TotalPowerRequired = totalPowerRequired,
            SensorCapability = totalSensorCapability,
            Cargo = new List<CargoEntry>(),
            IsAlive = true,
            HasFled = false
        };
    }

    /// <summary>
    /// Converts a FleetConfig into a list of CombatShipSnapshots.
    /// </summary>
    public static List<CombatShipSnapshot> CreateSnapshots(FleetConfig fleetConfig, Guid corpId, Guid fleetId)
    {
        var snapshots = new List<CombatShipSnapshot>();
        for (int i = 0; i < fleetConfig.Ships.Count; i++)
        {
            snapshots.Add(CreateSnapshot(fleetConfig.Ships[i], corpId, fleetId, i));
        }
        return snapshots;
    }

    /// <summary>
    /// Returns a list of known chassis names.
    /// </summary>
    public static IReadOnlyList<string> ListChassis() =>
        ChassisStats.Keys.OrderBy(k => k).ToList();

    /// <summary>
    /// Returns a list of known weapon names.
    /// </summary>
    public static IReadOnlyList<string> ListWeapons() =>
        WeaponStatsLookup.Keys.OrderBy(k => k).ToList();

    /// <summary>
    /// Returns a list of known module type names.
    /// </summary>
    public static IReadOnlyList<string> ListModules() =>
        ModuleStatsLookup.Keys.OrderBy(k => k).ToList();

    private static Guid GenerateDeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }

    private record ModuleStats(
        ModuleCapabilityType CapabilityType,
        decimal BaseValue,
        double PowerRequired,
        bool IsExterior,
        double ModuleMass);

    private record WeaponStats(
        DamageType DamageType,
        double BaseDamage,
        double WeaponMass);
}

using Google.Protobuf;
using psecsapi.Combat;

namespace psecsapi.Combat.Events;

/// <summary>
/// Collects combat events during simulation and serializes them to a compact
/// Protobuf binary stream for replay. Also provides deserialization for replay clients.
///
/// Wire format: sequence of (varint eventType, length-prefixed bytes eventData).
/// Each event is prefixed with its CombatEventType discriminator and the event data
/// as a length-prefixed ByteString.
/// </summary>
public class CombatEventRecorder
{
    private readonly List<CombatEvent> _events = new();

    /// <summary>
    /// Velocity change threshold (squared magnitude of delta) for position compression.
    /// Changes below this threshold are considered insignificant and do not trigger recording.
    /// Value of 1.0 means a combined velocity change of ~1 unit/tick in any direction.
    /// </summary>
    public const double VelocityChangeThresholdSquared = 1.0;

    /// <summary>Number of events recorded so far.</summary>
    public int EventCount => _events.Count;

    // === Recording Methods ===

    public void RecordCombatStarted(double gridWidth, double gridHeight, int randomSeed,
        List<TerrainObstacleRecord> terrain, List<ShipLoadoutRecord> shipLoadouts)
    {
        _events.Add(new CombatStartedEvent
        {
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            RandomSeed = randomSeed,
            Terrain = terrain,
            ShipLoadouts = shipLoadouts
        });
    }

    public void RecordShipMoved(Guid shipId, double newX, double newY,
        double velX, double velY, int tick)
    {
        _events.Add(new ShipMovedEvent
        {
            ShipId = shipId.ToString(),
            NewX = newX,
            NewY = newY,
            VelX = velX,
            VelY = velY,
            Tick = tick
        });
    }

    public void RecordWeaponFired(Guid shipId, Guid weaponId, double dirX, double dirY,
        Guid targetId, double targetX, double targetY, int tick)
    {
        _events.Add(new WeaponFiredEvent
        {
            ShipId = shipId.ToString(),
            WeaponId = weaponId.ToString(),
            DirX = dirX,
            DirY = dirY,
            TargetId = targetId.ToString(),
            TargetX = targetX,
            TargetY = targetY,
            Tick = tick
        });
    }

    public void RecordProjectileCreated(Guid projectileId, double originX, double originY,
        double velX, double velY, DamageType damageType, double damage, int tick)
    {
        _events.Add(new ProjectileCreatedEvent
        {
            ProjectileId = projectileId.ToString(),
            OriginX = originX,
            OriginY = originY,
            VelX = velX,
            VelY = velY,
            DamageType = (int)damageType,
            Damage = damage,
            Tick = tick
        });
    }

    public void RecordProjectileHit(Guid projectileId, Guid targetShipId,
        double damageDealt, double shieldAbsorbed, double armorAblated,
        double structureDamage, Guid moduleHitId, double moduleConditionDamage, int tick,
        string moduleHitName = "", double moduleConditionAfter = 0.0)
    {
        _events.Add(new ProjectileHitEvent
        {
            ProjectileId = projectileId.ToString(),
            TargetShipId = targetShipId.ToString(),
            DamageDealt = damageDealt,
            ShieldAbsorbed = shieldAbsorbed,
            ArmorAblated = armorAblated,
            StructureDamage = structureDamage,
            ModuleHitId = moduleHitId.ToString(),
            ModuleConditionDamage = moduleConditionDamage,
            Tick = tick,
            ModuleHitName = moduleHitName,
            ModuleConditionAfter = moduleConditionAfter
        });
    }

    public void RecordShipDestroyed(Guid shipId, Guid destroyerShipId,
        double posX, double posY, List<Guid> cargoDropped, int tick)
    {
        _events.Add(new ShipDestroyedEvent
        {
            ShipId = shipId.ToString(),
            DestroyerShipId = destroyerShipId.ToString(),
            PosX = posX,
            PosY = posY,
            CargoDropped = cargoDropped.Select(c => c.ToString()).ToList(),
            Tick = tick
        });
    }

    public void RecordShipFled(Guid shipId, double exitX, double exitY, int tick)
    {
        _events.Add(new ShipFledEvent
        {
            ShipId = shipId.ToString(),
            ExitX = exitX,
            ExitY = exitY,
            Tick = tick
        });
    }

    public void RecordModuleDestroyed(Guid shipId, Guid moduleId, int tick,
        string moduleName = "")
    {
        _events.Add(new ModuleDestroyedEvent
        {
            ShipId = shipId.ToString(),
            ModuleId = moduleId.ToString(),
            Tick = tick,
            ModuleName = moduleName
        });
    }

    public void RecordEnvironmentalDamage(Guid shipId, string sourceType, double damage, int tick)
    {
        _events.Add(new EnvironmentalDamageEvent
        {
            ShipId = shipId.ToString(),
            SourceType = sourceType,
            Damage = damage,
            Tick = tick
        });
    }

    public void RecordCombatEnded(CombatOutcome outcome, List<Guid> survivingShips,
        int tickCount, double durationSeconds)
    {
        _events.Add(new CombatEndedEvent
        {
            Outcome = (int)outcome,
            SurvivingShips = survivingShips.Select(s => s.ToString()).ToList(),
            TickCount = tickCount,
            DurationSeconds = durationSeconds
        });
    }

    // === Serialization ===

    /// <summary>
    /// Serialize all recorded events to a binary byte array.
    /// Wire format per event: [varint eventType] [length-prefixed event data bytes]
    /// </summary>
    public byte[] Serialize()
    {
        if (_events.Count == 0)
            return Array.Empty<byte>();

        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        foreach (var evt in _events)
        {
            var eventData = evt.Serialize();
            output.WriteInt32((int)evt.EventType);
            output.WriteBytes(ByteString.CopyFrom(eventData));
        }

        output.Flush();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Deserialize a binary byte array back into a list of combat events.
    /// </summary>
    public static List<CombatEvent> Deserialize(byte[] data)
    {
        var events = new List<CombatEvent>();

        if (data == null || data.Length == 0)
            return events;

        using var input = new CodedInputStream(data);

        while (!input.IsAtEnd)
        {
            var eventTypeInt = input.ReadInt32();
            var eventType = (CombatEventType)eventTypeInt;
            var eventData = input.ReadBytes().ToByteArray();

            CombatEvent evt = eventType switch
            {
                CombatEventType.CombatStarted => CombatStartedEvent.Deserialize(eventData),
                CombatEventType.ShipMoved => ShipMovedEvent.Deserialize(eventData),
                CombatEventType.WeaponFired => WeaponFiredEvent.Deserialize(eventData),
                CombatEventType.ProjectileCreated => ProjectileCreatedEvent.Deserialize(eventData),
                CombatEventType.ProjectileHit => ProjectileHitEvent.Deserialize(eventData),
                CombatEventType.ShipDestroyed => ShipDestroyedEvent.Deserialize(eventData),
                CombatEventType.ShipFled => ShipFledEvent.Deserialize(eventData),
                CombatEventType.ModuleDestroyed => ModuleDestroyedEvent.Deserialize(eventData),
                CombatEventType.EnvironmentalDamage => EnvironmentalDamageEvent.Deserialize(eventData),
                CombatEventType.CombatEnded => CombatEndedEvent.Deserialize(eventData),
                _ => throw new InvalidOperationException($"Unknown combat event type: {eventTypeInt}")
            };

            events.Add(evt);
        }

        return events;
    }

    // === Position Compression ===

    /// <summary>
    /// Determines whether a ship's position should be recorded this tick.
    /// Per spec: record only on compute tick, significant velocity change,
    /// damage taken, or terrain collision.
    /// </summary>
    public static bool ShouldRecordPosition(bool isComputeTick, bool velocityChanged,
        bool tookDamage, bool terrainCollision)
    {
        return isComputeTick || velocityChanged || tookDamage || terrainCollision;
    }

    /// <summary>
    /// Determines whether a velocity change is significant enough to trigger position recording.
    /// Uses squared magnitude of the velocity delta to avoid a sqrt call.
    /// </summary>
    public static bool IsSignificantVelocityChange(double oldVelX, double oldVelY,
        double newVelX, double newVelY)
    {
        var dx = newVelX - oldVelX;
        var dy = newVelY - oldVelY;
        var magnitudeSquared = dx * dx + dy * dy;
        return magnitudeSquared >= VelocityChangeThresholdSquared;
    }
}

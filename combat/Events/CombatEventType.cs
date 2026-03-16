namespace psecsapi.Combat.Events;

/// <summary>
/// Identifies the type of combat event in the replay stream.
/// Integer values are used as discriminator tags in the binary format.
/// </summary>
public enum CombatEventType
{
    CombatStarted = 0,
    ShipMoved = 1,
    WeaponFired = 2,
    ProjectileCreated = 3,
    ProjectileHit = 4,
    ShipDestroyed = 5,
    ShipFled = 6,
    ModuleDestroyed = 7,
    EnvironmentalDamage = 8,
    CombatEnded = 9
}

using Orleans;

namespace psecsapi.Combat.Snapshots;

/// <summary>
/// Frozen snapshot of a weapon module for combat simulation.
/// Captures all combat-relevant weapon properties at the moment of snapshot creation.
/// </summary>
[GenerateSerializer]
public record class WeaponSnapshot
{
    /// <summary>Module instance ID from the TechModule.</summary>
    [Id(0)] public Guid ModuleId { get; init; }

    /// <summary>The damage type this weapon deals (Energy or Kinetic).</summary>
    [Id(1)] public DamageType DamageType { get; init; }

    /// <summary>Base damage value from the module's EnergyDamage or KineticDamage capability.</summary>
    [Id(2)] public double BaseDamage { get; init; }

    /// <summary>Number of simulation ticks between shots. Determined by weapon type via WeaponCooldownMapper.</summary>
    [Id(3)] public int CooldownTicks { get; init; }

    /// <summary>Projectile travel speed in units per tick. 0 for energy weapons (hitscan).</summary>
    [Id(4)] public double ProjectileSpeed { get; init; }

    /// <summary>Maximum effective range in grid units. Derived from the ship's targeting modules (WeaponRange capability) plus a default base range.</summary>
    [Id(5)] public double Range { get; init; }

    /// <summary>Accuracy cone angle in radians derived from the module's Sensitivity quality property.</summary>
    [Id(6)] public double ConeAngle { get; init; }

    /// <summary>Current condition of the weapon module (0-100). Affects effective damage output.</summary>
    [Id(7)] public decimal Condition { get; init; }
}

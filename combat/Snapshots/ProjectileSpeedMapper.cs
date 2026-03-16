namespace psecsapi.Combat.Snapshots;

/// <summary>
/// Maps weapon module names to projectile travel speeds in units per simulation tick.
/// Energy weapons return 0 (hitscan - instant hit, no projectile travel).
/// Kinetic weapons return positive speeds; higher tiers have faster projectiles.
/// </summary>
public static class ProjectileSpeedMapper
{
    private static readonly Dictionary<string, double> SpeedsByWeaponName = new(StringComparer.OrdinalIgnoreCase)
    {
        // Kinetic weapons (T2-T7) - projectile travel; higher tiers are faster
        ["Autocannon"] = 150.0,
        ["MassDriver"] = 200.0,
        ["Railgun"] = 225.0,
        ["GaussLance"] = 250.0,
        ["CoilgunArray"] = 300.0,
        ["NovaDriver"] = 400.0,

        // Energy weapons (T3-T7) - hitscan (instant hit, no projectile travel)
        ["PulseLaser"] = 0.0,
        ["BeamCannon"] = 0.0,
        ["PlasmaLance"] = 0.0,
        ["DisruptorArray"] = 0.0,
        ["NovaProjector"] = 0.0,
    };

    /// <summary>
    /// Returns the projectile speed for the given weapon module name.
    /// Returns 0.0 (hitscan) for energy weapons and unrecognized names.
    /// </summary>
    public static double GetProjectileSpeed(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName))
            return 0.0;

        return SpeedsByWeaponName.TryGetValue(weaponName, out var speed)
            ? speed
            : 0.0;
    }
}

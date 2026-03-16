namespace psecsapi.Combat.Snapshots;

/// <summary>
/// Maps weapon module names to cooldown durations in simulation ticks.
/// Energy weapons (T3 PulseLaser through T7 NovaProjector): hitscan, moderate cooldowns.
/// Kinetic weapons (T2 Autocannon through T7 NovaDriver): projectile travel, range from fast to very slow.
/// Unrecognized weapon names fall back to DefaultCooldown (10 ticks).
/// </summary>
public static class WeaponCooldownMapper
{
    private static readonly Dictionary<string, int> CooldownsByWeaponName = new(StringComparer.OrdinalIgnoreCase)
    {
        // Energy weapons (T3-T7): hitscan, cooldown increases with tier due to charge-up time
        ["PulseLaser"] = 8,
        ["BeamCannon"] = 12,
        ["PlasmaLance"] = 15,
        ["DisruptorArray"] = 20,
        ["NovaProjector"] = 25,

        // Kinetic weapons (T2-T7): projectile travel, cooldown increases with mass/recoil
        ["Autocannon"] = 3,
        ["MassDriver"] = 6,
        ["Railgun"] = 10,
        ["GaussLance"] = 15,
        ["CoilgunArray"] = 20,
        ["NovaDriver"] = 30,
    };

    /// <summary>Default cooldown for unrecognized weapon types.</summary>
    public const int DefaultCooldown = 10;

    /// <summary>
    /// Returns the cooldown in simulation ticks for the given weapon module name.
    /// Returns DefaultCooldown (10) for unrecognized weapon names.
    /// </summary>
    public static int GetCooldownTicks(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName))
            return DefaultCooldown;

        return CooldownsByWeaponName.TryGetValue(weaponName, out var cooldown)
            ? cooldown
            : DefaultCooldown;
    }
}

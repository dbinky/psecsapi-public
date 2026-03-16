namespace psecsapi.Combat.Weapons;

/// <summary>
/// Tracks per-weapon cooldown timers. Each weapon has a cooldown (in simulation ticks)
/// after firing before it can fire again. The tracker is decremented once per simulation tick.
/// </summary>
public class WeaponCooldownTracker
{
    private readonly Dictionary<Guid, int> _cooldowns = new();

    /// <summary>
    /// Decrements all active cooldowns by 1. Cooldowns at 0 remain at 0.
    /// Called once per simulation tick.
    /// </summary>
    public void DecrementAll()
    {
        var keys = _cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0)
            {
                _cooldowns[key]--;
            }
        }
    }

    /// <summary>
    /// Sets the cooldown for a weapon after it fires.
    /// </summary>
    /// <param name="weaponId">The unique ID of the weapon module.</param>
    /// <param name="cooldownTicks">Number of ticks before the weapon can fire again.</param>
    public void SetCooldown(Guid weaponId, int cooldownTicks)
    {
        _cooldowns[weaponId] = cooldownTicks;
    }

    /// <summary>
    /// Returns true if the weapon is ready to fire (cooldown is 0 or not tracked).
    /// </summary>
    public bool IsReady(Guid weaponId)
    {
        return !_cooldowns.TryGetValue(weaponId, out int remaining) || remaining <= 0;
    }

    /// <summary>
    /// Returns the remaining cooldown ticks for a weapon, or 0 if not tracked.
    /// </summary>
    public int GetRemainingCooldown(Guid weaponId)
    {
        return _cooldowns.TryGetValue(weaponId, out int remaining) ? remaining : 0;
    }

    /// <summary>
    /// Returns the total number of weapons currently being tracked.
    /// </summary>
    public int TrackedWeaponCount => _cooldowns.Count;
}

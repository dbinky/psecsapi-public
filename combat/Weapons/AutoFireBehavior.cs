using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Weapons;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Between compute ticks, ships automatically fire at enemies using a simple target
/// selection algorithm: pick the nearest alive enemy within weapon range. This provides
/// baseline combat behavior for ships whose scripts have not yet executed or have no script.
/// </summary>
public static class AutoFireBehavior
{
    /// <summary>
    /// Selects the best auto-fire target for a ship: the nearest alive enemy within any weapon's range.
    /// Returns null if no valid target exists.
    ///
    /// Selection criteria:
    /// 1. Enemy must be alive (CurrentStructurePoints > 0).
    /// 2. Enemy must be within range of at least one of the ship's weapons.
    /// 3. Among qualifying enemies, select the nearest one.
    /// </summary>
    /// <param name="ship">The ship that is auto-firing.</param>
    /// <param name="enemies">All enemy ships (may include dead ones).</param>
    /// <returns>The nearest alive enemy within weapon range, or null.</returns>
    public static CombatShipSnapshot? SelectAutoFireTarget(
        CombatShipSnapshot ship,
        List<CombatShipSnapshot> enemies)
    {
        if (ship.Weapons == null || ship.Weapons.Count == 0)
        {
            return null;
        }

        // Find the maximum range among the ship's functional weapons
        double maxRange = 0;
        foreach (var weapon in ship.Weapons)
        {
            if (weapon.Condition > 0 && weapon.Range > maxRange)
            {
                maxRange = weapon.Range;
            }
        }

        if (maxRange <= 0)
        {
            return null;
        }

        CombatShipSnapshot? nearestTarget = null;
        double nearestDistSq = double.MaxValue;

        foreach (var enemy in enemies)
        {
            // Skip dead enemies
            if (enemy.CurrentStructurePoints <= 0) continue;

            double distSq = DistanceSquared(ship.Position, enemy.Position);

            // Must be within max weapon range
            if (distSq > maxRange * maxRange) continue;

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestTarget = enemy;
            }
        }

        return nearestTarget;
    }

    /// <summary>
    /// Selects the best weapon to fire at a specific target, considering cooldown,
    /// condition, range, and damage type effectiveness.
    ///
    /// Priority:
    /// 1. Must be ready (cooldown 0) and functional (condition > 0).
    /// 2. Target must be within weapon range.
    /// 3. Among qualifying weapons, prefer energy weapons against low-armor targets
    ///    and kinetic weapons against low-shield targets (rock-paper-scissors).
    /// 4. Tie-break: highest damage weapon.
    /// </summary>
    public static WeaponSnapshot? SelectBestWeapon(
        CombatShipSnapshot ship,
        CombatShipSnapshot target,
        WeaponCooldownTracker cooldownTracker)
    {
        if (ship.Weapons == null || ship.Weapons.Count == 0)
        {
            return null;
        }

        double distance = Distance(ship.Position, target.Position);

        WeaponSnapshot? bestWeapon = null;
        double bestScore = -1;

        foreach (var weapon in ship.Weapons)
        {
            // Skip destroyed weapons
            if (weapon.Condition <= 0) continue;

            // Skip weapons on cooldown
            if (!cooldownTracker.IsReady(weapon.ModuleId)) continue;

            // Skip weapons whose range cannot reach
            // Kinetic weapons have no range limit (projectile travels), but we still
            // prefer weapons that are "in range" for auto-fire targeting purposes.
            // For energy weapons, range is a hard limit.
            if (weapon.DamageType == DamageType.Energy && weapon.Range < distance)
            {
                continue;
            }

            // Score: base damage, with a bonus for type advantage
            double score = weapon.BaseDamage;

            // Rock-paper-scissors: prefer the type the target is weak against.
            // Compare total resistance capacities (not condition-degraded effectiveness ratios),
            // so the advantage is based on the target's defensive loadout, not its current damage state.
            if (weapon.DamageType == DamageType.Energy && target.TotalKineticResistance > target.TotalEnergyResistance)
            {
                // Target has more kinetic (armor) than energy (shield) resistance --
                // energy damage bypasses armor, so it is the better choice here.
                score *= 1.25;
            }
            else if (weapon.DamageType == DamageType.Kinetic && target.TotalEnergyResistance > target.TotalKineticResistance)
            {
                // Target has more energy (shield) than kinetic (armor) resistance --
                // kinetic damage bypasses shields, so it is the better choice here.
                score *= 1.25;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestWeapon = weapon;
            }
        }

        return bestWeapon;
    }

    private static double DistanceSquared(Vector2D a, Vector2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double Distance(Vector2D a, Vector2D b)
    {
        return Math.Sqrt(DistanceSquared(a, b));
    }
}

using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Weapons;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Resolves an energy weapon (hitscan) firing attempt. Energy beams are instant -- the ray
/// either hits or misses in a single tick. Damage attenuates with distance: full damage within
/// 25% of max range, linear falloff to 25% damage at max range.
/// </summary>
public static class EnergyWeaponResolver
{
    /// <summary>
    /// Fires an energy weapon from shooter at target. Energy weapons are instant hitscan --
    /// the beam reaches the target (or doesn't) in the same tick.
    ///
    /// Steps:
    /// 1. Check weapon cooldown via tracker -- must be ready.
    /// 2. Check weapon condition -- must be > 0 (not destroyed).
    /// 3. Check range -- distance to target must be &lt;= weapon range.
    /// 4. Calculate accuracy cone from weapon's base cone angle + environment.
    /// 5. Apply cone deviation to aim direction using seeded RNG.
    /// 6. Check line-of-sight from shooter to target (energy mode: all obstacles block).
    /// 7. If LOS clear, check if deviated ray intersects target hitbox circle.
    /// 8. On hit: apply distance attenuation, then run DamagePipeline.
    /// </summary>
    public static EnergyFireResult Fire(
        WeaponSnapshot weapon,
        CombatShipSnapshot shooter,
        CombatShipSnapshot target,
        CombatGrid grid,
        bool isNebula,
        bool shooterInDensePatch,
        WeaponCooldownTracker cooldownTracker,
        Random rng)
    {
        // Step 1: Cooldown check
        if (!cooldownTracker.IsReady(weapon.ModuleId))
        {
            return new EnergyFireResult(false, null, Vector2D.Zero, 0, 0);
        }

        // Step 2: Condition check -- destroyed weapons cannot fire
        if (weapon.Condition <= 0)
        {
            return new EnergyFireResult(false, null, Vector2D.Zero, 0, 0);
        }

        // Step 3: Range check
        double distance = Distance(shooter.Position, target.Position);
        double effectiveRange = weapon.Range;
        if (effectiveRange <= 0) return new EnergyFireResult(false, null, Vector2D.Zero, distance, 0);

        if (distance > effectiveRange)
        {
            return new EnergyFireResult(false, null, Vector2D.Zero, distance, 0);
        }

        // Step 4: Calculate accuracy cone (base cone from snapshot, apply environment multipliers)
        double coneAngle = weapon.ConeAngle;
        if (isNebula)
        {
            coneAngle *= CombatConstants.NebulaConeMultiplier;
        }
        if (shooterInDensePatch)
        {
            coneAngle *= CombatConstants.DensePatchConeMultiplier;
        }

        // Step 5: Calculate aim direction and apply cone deviation
        Vector2D aimDirection = Normalize(
            new Vector2D(target.Position.X - shooter.Position.X,
                         target.Position.Y - shooter.Position.Y));
        Vector2D fireDirection = AccuracyConeCalculator.ApplyConeDeviation(aimDirection, coneAngle, rng);

        // Step 6: Check line-of-sight (energy mode -- all obstacles block)
        if (LineOfSightChecker.IsBlocked(shooter.Position, target.Position, grid, isKinetic: false))
        {
            return new EnergyFireResult(false, null, fireDirection, distance, 0);
        }

        // Step 7: Check if deviated ray intersects target hitbox
        double targetHitbox = CalculateHitboxRadius(target.Mass);
        bool rayHitsTarget = RayIntersectsCircle(
            shooter.Position, fireDirection, target.Position, targetHitbox, distance);

        if (!rayHitsTarget)
        {
            return new EnergyFireResult(false, null, fireDirection, distance, 0);
        }

        // Step 8: Apply distance attenuation and resolve damage
        double attenuatedDamage = CalculateDistanceAttenuation(weapon.BaseDamage, distance, effectiveRange);
        DamageResult damageResult = DamagePipeline.Resolve(attenuatedDamage, DamageType.Energy, target, rng);

        return new EnergyFireResult(true, damageResult, fireDirection, distance, attenuatedDamage);
    }

    /// <summary>
    /// Calculates energy weapon distance attenuation.
    /// Full damage within 25% of max range.
    /// Linear falloff from 100% to 25% between 25% and 100% of max range.
    /// </summary>
    public static double CalculateDistanceAttenuation(double baseDamage, double distance, double maxRange)
    {
        double fullDamageRange = maxRange * CombatConstants.EnergyFullDamageRangePercent;

        if (distance <= fullDamageRange)
        {
            return baseDamage;
        }

        // Linear interpolation from 100% at fullDamageRange to EnergyMinDamagePercent at maxRange
        double t = (distance - fullDamageRange) / (maxRange - fullDamageRange);
        double damageMultiplier = 1.0 - t * (1.0 - CombatConstants.EnergyMinDamagePercent);

        return baseDamage * damageMultiplier;
    }

    /// <summary>
    /// Calculates ship hitbox radius from mass.
    /// Formula: 20 + sqrt(mass) * 0.5
    /// </summary>
    public static double CalculateHitboxRadius(double mass)
    {
        if (mass <= 0) return 20.0; // minimum hitbox for zero/negative mass
        return 20.0 + Math.Sqrt(mass) * 0.5;
    }

    /// <summary>
    /// Checks if a ray from origin in the given direction intersects a circle.
    /// The ray is bounded by maxDistance (weapon range).
    ///
    /// Algorithm:
    /// 1. Compute the vector from ray origin to circle center.
    /// 2. Project it onto the ray direction to find the closest approach point.
    /// 3. If the closest approach distance is less than the circle radius, the ray hits.
    /// 4. Ensure the intersection is within [0, maxDistance] along the ray.
    /// </summary>
    public static bool RayIntersectsCircle(
        Vector2D origin, Vector2D direction, Vector2D circleCenter, double radius, double maxDistance)
    {
        double ocX = circleCenter.X - origin.X;
        double ocY = circleCenter.Y - origin.Y;

        // Project OC onto the ray direction (direction is unit vector)
        double projLength = ocX * direction.X + ocY * direction.Y;

        // Closest approach: perpendicular distance squared from circle center to ray
        double ocLenSq = ocX * ocX + ocY * ocY;
        double closestDistSq = ocLenSq - projLength * projLength;

        double radiusSq = radius * radius;

        // If closest approach is farther than radius, ray misses
        if (closestDistSq > radiusSq)
        {
            return false;
        }

        // Find the distance along the ray to the intersection points
        double halfChord = Math.Sqrt(radiusSq - closestDistSq);
        double t1 = projLength - halfChord; // entry point
        double t2 = projLength + halfChord; // exit point

        // Ray hits if any intersection point is within [0, maxDistance]
        // t2 < 0 means circle is entirely behind the ray
        if (t2 < 0) return false;

        // t1 > maxDistance means circle is entirely beyond range
        if (t1 > maxDistance) return false;

        return true;
    }

    private static double Distance(Vector2D a, Vector2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Vector2D Normalize(Vector2D v)
    {
        double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        if (len < 1e-10) return Vector2D.Zero;
        return new Vector2D(v.X / len, v.Y / len);
    }
}

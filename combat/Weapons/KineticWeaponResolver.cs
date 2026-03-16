using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;
using psecsapi.Combat.Weapons;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Resolves a kinetic weapon firing attempt. Unlike energy weapons, kinetic weapons create
/// a projectile that travels across the grid over subsequent simulation ticks. There is no
/// immediate hit check and no distance attenuation -- if the projectile eventually collides
/// with a target, it deals full damage.
/// </summary>
public static class KineticWeaponResolver
{
    /// <summary>
    /// Fires a kinetic weapon from shooter toward target. Creates a projectile that will
    /// travel across the grid in subsequent simulation ticks.
    ///
    /// Steps:
    /// 1. Check weapon cooldown via tracker -- must be ready.
    /// 2. Check weapon condition -- must be > 0 (not destroyed).
    /// 3. Calculate accuracy cone from weapon's base cone angle + environment.
    /// 4. Apply cone deviation to aim direction.
    /// 5. Create projectile at shooter position, traveling in deviated direction at weapon's projectile speed.
    ///
    /// No range check -- kinetic projectiles travel until they hit something or leave the grid.
    /// No distance attenuation -- full damage if the projectile collides with a target.
    /// No LOS check at fire time -- the projectile will be checked for obstacle collisions as it travels.
    /// </summary>
    public static KineticFireResult Fire(
        WeaponSnapshot weapon,
        CombatShipSnapshot shooter,
        CombatShipSnapshot target,
        bool isNebula,
        bool shooterInDensePatch,
        WeaponCooldownTracker cooldownTracker,
        Random rng)
    {
        // Step 1: Cooldown check
        if (!cooldownTracker.IsReady(weapon.ModuleId))
        {
            return new KineticFireResult(false, null, Vector2D.Zero);
        }

        // Step 2: Condition check -- destroyed weapons cannot fire
        if (weapon.Condition <= 0)
        {
            return new KineticFireResult(false, null, Vector2D.Zero);
        }

        // Step 3: Calculate accuracy cone (base cone from snapshot, apply environment multipliers)
        double coneAngle = weapon.ConeAngle;
        if (isNebula)
        {
            coneAngle *= CombatConstants.NebulaConeMultiplier;
        }
        if (shooterInDensePatch)
        {
            coneAngle *= CombatConstants.DensePatchConeMultiplier;
        }

        // Step 4: Calculate aim direction and apply cone deviation
        Vector2D aimDirection = Normalize(
            new Vector2D(target.Position.X - shooter.Position.X,
                         target.Position.Y - shooter.Position.Y));
        Vector2D fireDirection = AccuracyConeCalculator.ApplyConeDeviation(aimDirection, coneAngle, rng);

        // Step 5: Create projectile
        double projectileSpeed = weapon.ProjectileSpeed > 0
            ? weapon.ProjectileSpeed
            : CombatConstants.BaseProjectileSpeed;

        Vector2D velocity = new Vector2D(
            fireDirection.X * projectileSpeed,
            fireDirection.Y * projectileSpeed);

        var projectile = new Projectile(
            id: Guid.NewGuid(),
            origin: shooter.Position,
            velocity: velocity,
            speed: projectileSpeed,
            damageType: DamageType.Kinetic,
            damage: weapon.BaseDamage,
            sourceShipId: shooter.ShipId,
            targetShipId: target.ShipId);

        return new KineticFireResult(true, projectile, fireDirection);
    }

    private static Vector2D Normalize(Vector2D v)
    {
        double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        if (len < 1e-10) return Vector2D.Zero;
        return new Vector2D(v.X / len, v.Y / len);
    }
}

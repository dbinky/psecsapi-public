using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Weapons;

/// <summary>
/// Checks whether a projectile at its current position has collided with any ship or been
/// destroyed by a large obstacle. Called each simulation tick after projectiles advance.
/// </summary>
public static class ProjectileCollisionResolver
{
    /// <summary>
    /// Checks if a projectile has collided with any ship or been destroyed by a large obstacle.
    ///
    /// Ship collision: projectile position is within the ship's hitbox radius.
    /// Obstacle collision: kinetic projectiles are destroyed by obstacles with radius > SmallObstacleMaxRadius.
    /// Small obstacles (radius &lt;= 100) are ignored -- kinetic rounds pass through them.
    ///
    /// The source ship is excluded from collision checks (a ship cannot shoot itself).
    /// Dead ships (CurrentStructurePoints &lt;= 0) are excluded from collision checks.
    /// </summary>
    /// <param name="projectile">The projectile to check.</param>
    /// <param name="ships">All ships on the grid (both sides).</param>
    /// <param name="grid">Combat grid with obstacles.</param>
    /// <returns>Collision result indicating what was hit, if anything.</returns>
    public static ProjectileCollisionResult CheckCollisions(
        Projectile projectile,
        List<CombatShipSnapshot> ships,
        CombatGrid grid)
    {
        // Check obstacle collisions first (projectile may be destroyed before reaching a ship)
        foreach (var obstacle in grid.Obstacles)
        {
            // Kinetic projectiles pass through small obstacles
            if (obstacle.Radius <= CombatConstants.SmallObstacleMaxRadius)
            {
                continue;
            }

            double distSq = DistanceSquared(projectile.Position, obstacle.Position);
            if (distSq <= obstacle.Radius * obstacle.Radius)
            {
                return new ProjectileCollisionResult(null, true, obstacle);
            }
        }

        // Check ship collisions
        foreach (var ship in ships)
        {
            // Skip the source ship
            if (ship.ShipId == projectile.SourceShipId) continue;

            // Skip dead ships
            if (ship.CurrentStructurePoints <= 0) continue;

            double hitboxRadius = EnergyWeaponResolver.CalculateHitboxRadius(ship.Mass);
            double distSq = DistanceSquared(projectile.Position, ship.Position);

            if (distSq <= hitboxRadius * hitboxRadius)
            {
                return new ProjectileCollisionResult(ship, false, null);
            }
        }

        return new ProjectileCollisionResult(null, false, null);
    }

    /// <summary>
    /// Checks collisions along the projectile's entire travel path for the current tick,
    /// not just at its final position. This prevents fast projectiles from tunneling
    /// through thin targets.
    ///
    /// Uses the projectile's previous position (before Advance) and current position
    /// (after Advance) as a line segment, then checks for circle-line intersections.
    /// </summary>
    /// <param name="projectile">The projectile to check.</param>
    /// <param name="previousPosition">Position before this tick's Advance().</param>
    /// <param name="ships">All ships on the grid.</param>
    /// <param name="grid">Combat grid with obstacles.</param>
    /// <returns>Collision result indicating what was hit, if anything.</returns>
    public static ProjectileCollisionResult CheckCollisionsSweep(
        Projectile projectile,
        Vector2D previousPosition,
        List<CombatShipSnapshot> ships,
        CombatGrid grid)
    {
        // Check obstacle collisions along the path
        foreach (var obstacle in grid.Obstacles)
        {
            if (obstacle.Radius <= CombatConstants.SmallObstacleMaxRadius)
            {
                continue;
            }

            if (LineOfSightChecker.LineSegmentIntersectsCircle(
                previousPosition, projectile.Position, obstacle.Position, obstacle.Radius))
            {
                return new ProjectileCollisionResult(null, true, obstacle);
            }
        }

        // Check ship collisions along the path
        double bestDistSq = double.MaxValue;
        CombatShipSnapshot? hitShip = null;

        foreach (var ship in ships)
        {
            if (ship.ShipId == projectile.SourceShipId) continue;
            if (ship.CurrentStructurePoints <= 0) continue;

            double hitboxRadius = EnergyWeaponResolver.CalculateHitboxRadius(ship.Mass);

            if (LineOfSightChecker.LineSegmentIntersectsCircle(
                previousPosition, projectile.Position, ship.Position, hitboxRadius))
            {
                // Pick the ship closest to the previous position (first hit along path)
                double distSq = DistanceSquared(previousPosition, ship.Position);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    hitShip = ship;
                }
            }
        }

        if (hitShip != null)
        {
            return new ProjectileCollisionResult(hitShip, false, null);
        }

        return new ProjectileCollisionResult(null, false, null);
    }

    private static double DistanceSquared(Vector2D a, Vector2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}

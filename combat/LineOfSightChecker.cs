using psecsapi.Domain.Combat;

namespace psecsapi.Combat;

/// <summary>
/// Checks whether a line between two points is blocked by obstacles on the combat grid.
/// Energy weapons are blocked by ALL obstacles. Kinetic projectiles pass through small
/// obstacles (radius &lt;= SmallObstacleMaxRadius).
/// </summary>
public static class LineOfSightChecker
{
    /// <summary>
    /// Checks whether line-of-sight between two points is blocked by any obstacle.
    /// Energy weapons (isKinetic=false): blocked by ANY obstacle in the path.
    /// Kinetic projectiles (isKinetic=true): blocked only by obstacles with radius > SmallObstacleMaxRadius.
    /// </summary>
    /// <param name="from">Start point (shooter position).</param>
    /// <param name="to">End point (target position).</param>
    /// <param name="grid">Combat grid containing obstacles.</param>
    /// <param name="isKinetic">True for kinetic projectiles (pass through small obstacles).</param>
    /// <returns>True if LOS is blocked, false if clear.</returns>
    public static bool IsBlocked(Vector2D from, Vector2D to, CombatGrid grid, bool isKinetic)
    {
        foreach (var obstacle in grid.Obstacles)
        {
            // Kinetic projectiles pass through small obstacles
            if (isKinetic && obstacle.Radius <= CombatConstants.SmallObstacleMaxRadius)
            {
                continue;
            }

            if (LineSegmentIntersectsCircle(from, to, obstacle.Position, obstacle.Radius))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the first blocking obstacle, or null if LOS is clear.
    /// Useful for determining what blocked a shot (for event recording).
    /// </summary>
    public static CombatGridObstacle? GetBlockingObstacle(Vector2D from, Vector2D to, CombatGrid grid, bool isKinetic)
    {
        // Check obstacles sorted by distance from 'from' to find the nearest blocker
        double bestDistSq = double.MaxValue;
        CombatGridObstacle? nearest = null;

        foreach (var obstacle in grid.Obstacles)
        {
            if (isKinetic && obstacle.Radius <= CombatConstants.SmallObstacleMaxRadius)
            {
                continue;
            }

            if (LineSegmentIntersectsCircle(from, to, obstacle.Position, obstacle.Radius))
            {
                double distSq = DistanceSquared(from, obstacle.Position);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = obstacle;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Determines whether a line segment (from p1 to p2) intersects a circle
    /// centered at 'center' with the given radius.
    ///
    /// Algorithm:
    /// 1. Project the circle center onto the line defined by the segment.
    /// 2. Clamp the projection parameter t to [0, 1] to stay on the segment.
    /// 3. Compute the closest point on the segment to the circle center.
    /// 4. If the distance from that closest point to the center is less than radius, they intersect.
    /// </summary>
    public static bool LineSegmentIntersectsCircle(Vector2D p1, Vector2D p2, Vector2D center, double radius)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;

        double fx = p1.X - center.X;
        double fy = p1.Y - center.Y;

        double segLenSq = dx * dx + dy * dy;

        // Degenerate segment (zero length) -- just check point-in-circle
        if (segLenSq < 1e-10)
        {
            return (fx * fx + fy * fy) <= (radius * radius);
        }

        // Parameter t for the closest point on the infinite line to the center
        // t = -dot(f, d) / dot(d, d)
        double t = -(fx * dx + fy * dy) / segLenSq;

        // Clamp t to [0, 1] to stay on the segment
        t = Math.Clamp(t, 0.0, 1.0);

        // Closest point on segment
        double closestX = p1.X + t * dx;
        double closestY = p1.Y + t * dy;

        // Distance squared from closest point to circle center
        double distX = closestX - center.X;
        double distY = closestY - center.Y;
        double distSq = distX * distX + distY * distY;

        return distSq <= (radius * radius);
    }

    private static double DistanceSquared(Vector2D a, Vector2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}

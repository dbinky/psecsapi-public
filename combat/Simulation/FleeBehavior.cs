using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Handles flee behavior: calculating escape heading toward nearest grid boundary
/// and detecting when a ship has left the combat area.
/// </summary>
public static class FleeBehavior
{
    /// <summary>
    /// Calculate the flee direction and apply max thrust toward the nearest grid boundary.
    /// The ship heads toward whichever edge (left, right, top, bottom) is closest.
    /// </summary>
    public static void ExecuteFlee(CombatShipSnapshot ship)
    {
        Vector2D fleeTarget = CalculateFleeTarget(ship);
        double angle = Math.Atan2(
            fleeTarget.Y - ship.Position.Y,
            fleeTarget.X - ship.Position.X
        );
        ShipPhysics.ApplyThrust(ship, angle, 1.0); // Max thrust
    }

    /// <summary>
    /// Determine the nearest grid boundary point to flee toward.
    /// Returns a point beyond the edge to ensure the ship exits.
    /// </summary>
    public static Vector2D CalculateFleeTarget(CombatShipSnapshot ship)
    {
        double distLeft = Math.Abs(ship.Position.X - CombatConstants.GridMin);
        double distRight = Math.Abs(CombatConstants.GridMax - ship.Position.X);
        double distBottom = Math.Abs(ship.Position.Y - CombatConstants.GridMin);
        double distTop = Math.Abs(CombatConstants.GridMax - ship.Position.Y);

        double minDist = Math.Min(Math.Min(distLeft, distRight), Math.Min(distBottom, distTop));

        if (minDist == distLeft)
            return new Vector2D(CombatConstants.GridMin - 100, ship.Position.Y);
        if (minDist == distRight)
            return new Vector2D(CombatConstants.GridMax + 100, ship.Position.Y);
        if (minDist == distBottom)
            return new Vector2D(ship.Position.X, CombatConstants.GridMin - 100);

        return new Vector2D(ship.Position.X, CombatConstants.GridMax + 100);
    }

    /// <summary>
    /// Check whether the ship has crossed outside the grid bounds.
    /// If so, mark the ship as having fled.
    /// Returns true if the ship just crossed the boundary this check.
    /// </summary>
    public static bool CheckFled(CombatShipSnapshot ship, CombatGrid grid)
    {
        if (ship.HasFled) return false; // Already fled

        bool outsideBounds =
            ship.Position.X < CombatConstants.GridMin ||
            ship.Position.X > CombatConstants.GridMax ||
            ship.Position.Y < CombatConstants.GridMin ||
            ship.Position.Y > CombatConstants.GridMax;

        if (outsideBounds)
        {
            ship.HasFled = true;
            return true;
        }

        return false;
    }
}

using psecsapi.Domain.Combat;

namespace psecsapi.Combat;

public static class StartingPositionCalculator
{
    /// <summary>
    /// Calculates starting positions for both fleets.
    ///
    /// Starting distance = BaseStartDistance + (maxSensorCapability * SensorDistanceMultiplier).
    /// Attackers are placed on the left side (negative X), defenders on the right (positive X).
    /// Ships within each fleet are spread vertically around the fleet anchor point using FleetSpreadPerShip spacing.
    /// Positions that overlap with obstacles are nudged until clear.
    /// </summary>
    /// <param name="maxSensorCapability">The higher of the two fleets' aggregate sensor capability values.</param>
    /// <param name="attackerShipCount">Number of attacker ships to place.</param>
    /// <param name="defenderShipCount">Number of defender ships to place.</param>
    /// <param name="rng">Seeded Random for determinism.</param>
    /// <param name="obstacles">Obstacles on the grid to avoid.</param>
    /// <returns>Tuple of attacker positions and defender positions arrays.</returns>
    public static (Vector2D[] AttackerPositions, Vector2D[] DefenderPositions) CalculateStartingPositions(
        double maxSensorCapability,
        int attackerShipCount,
        int defenderShipCount,
        Random rng,
        CombatGridObstacle[] obstacles)
    {
        double startDistance = CombatConstants.BaseStartDistance
            + (maxSensorCapability * CombatConstants.SensorDistanceMultiplier);

        // Clamp so fleets don't start outside grid bounds
        double halfGrid = CombatConstants.GridMax;
        double clampedHalf = Math.Min(startDistance / 2.0, halfGrid - 500.0);

        double attackerAnchorX = -clampedHalf;
        double defenderAnchorX = clampedHalf;

        var attackerPositions = SpreadShips(attackerAnchorX, attackerShipCount, rng, obstacles);
        var defenderPositions = SpreadShips(defenderAnchorX, defenderShipCount, rng, obstacles);

        return (attackerPositions, defenderPositions);
    }

    /// <summary>
    /// Spreads ships vertically around the anchor X position.
    /// Ships are centered vertically at Y=0 with FleetSpreadPerShip spacing.
    /// Small random jitter is added to prevent perfectly predictable placement.
    /// </summary>
    private static Vector2D[] SpreadShips(
        double anchorX,
        int shipCount,
        Random rng,
        CombatGridObstacle[] obstacles)
    {
        var positions = new Vector2D[shipCount];
        double totalSpread = (shipCount - 1) * CombatConstants.FleetSpreadPerShip;
        double startY = -totalSpread / 2.0;

        for (int i = 0; i < shipCount; i++)
        {
            double baseY = startY + (i * CombatConstants.FleetSpreadPerShip);

            // Small jitter: +/- 50 units on each axis
            double jitterX = (rng.NextDouble() - 0.5) * 100.0;
            double jitterY = (rng.NextDouble() - 0.5) * 100.0;

            var candidate = new Vector2D(anchorX + jitterX, baseY + jitterY);

            // Clamp to grid bounds
            candidate = ClampToGrid(candidate);

            // Nudge away from obstacles
            candidate = NudgeFromObstacles(candidate, obstacles, rng);

            positions[i] = candidate;
        }

        return positions;
    }

    /// <summary>
    /// Clamps a position to within the grid boundaries with a margin.
    /// </summary>
    private static Vector2D ClampToGrid(Vector2D position)
    {
        double margin = 200.0;
        double x = Math.Clamp(position.X, CombatConstants.GridMin + margin, CombatConstants.GridMax - margin);
        double y = Math.Clamp(position.Y, CombatConstants.GridMin + margin, CombatConstants.GridMax - margin);
        return new Vector2D(x, y);
    }

    /// <summary>
    /// If a position overlaps with any obstacle, nudge it in a random direction
    /// until it is clear. Gives up after 10 attempts and returns best effort.
    /// </summary>
    private static Vector2D NudgeFromObstacles(Vector2D position, CombatGridObstacle[] obstacles, Random rng)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            bool overlapping = false;
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i].Position.DistanceTo(position) <= obstacles[i].Radius + 50.0)
                {
                    overlapping = true;
                    // Nudge in a random direction by the obstacle radius + buffer
                    double angle = rng.NextDouble() * 2.0 * Math.PI;
                    double nudgeDist = obstacles[i].Radius + 100.0;
                    position = new Vector2D(
                        position.X + Math.Cos(angle) * nudgeDist,
                        position.Y + Math.Sin(angle) * nudgeDist);
                    position = ClampToGrid(position);
                    break;
                }
            }
            if (!overlapping)
                break;
        }
        return position;
    }
}

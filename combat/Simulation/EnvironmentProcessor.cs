using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Processes environmental hazards during combat simulation.
/// Checks black hole gravity, event horizon, star heat, and obstacle collisions.
/// </summary>
public static class EnvironmentProcessor
{
    /// <summary>
    /// Process all environmental effects for a single ship on this tick.
    /// Returns a list of damage events (may be empty if no hazards apply).
    /// Mutates ship state directly (velocity for gravity, structure for damage, IsAlive for kills).
    /// </summary>
    public static List<EnvironmentalDamageEvent> ProcessEnvironment(
        CombatShipSnapshot ship,
        CombatGrid grid)
    {
        var events = new List<EnvironmentalDamageEvent>();

        if (!ship.IsAlive || ship.HasFled) return events;

        foreach (var obstacle in grid.Obstacles)
        {
            switch (obstacle.Type)
            {
                case ObstacleType.EventHorizon:
                    ProcessEventHorizon(ship, obstacle, events);
                    if (!ship.IsAlive) return events; // Ship destroyed -- no further processing
                    ProcessBlackHoleGravity(ship, obstacle);
                    break;

                case ObstacleType.Star:
                    ProcessStarHeat(ship, obstacle, events);
                    ProcessCollision(ship, obstacle, events);
                    break;

                case ObstacleType.Asteroid:
                case ObstacleType.Planet:
                    ProcessCollision(ship, obstacle, events);
                    break;
            }
        }

        return events;
    }

    /// <summary>
    /// If the ship is inside the event horizon radius, it is instantly destroyed.
    /// </summary>
    private static void ProcessEventHorizon(
        CombatShipSnapshot ship,
        CombatGridObstacle obstacle,
        List<EnvironmentalDamageEvent> events)
    {
        double distance = ship.Position.DistanceTo(obstacle.Position);
        if (distance <= obstacle.Radius)
        {
            ship.CurrentStructurePoints = 0;
            ship.IsAlive = false;
            events.Add(new EnvironmentalDamageEvent
            {
                ShipId = ship.ShipId,
                SourceType = "EventHorizon",
                DamageDealt = (double)ship.MaxStructurePoints,
                ShipDestroyed = true,
                ShipPosition = ship.Position
            });
        }
    }

    /// <summary>
    /// Apply gravitational acceleration toward the black hole center.
    /// Gravity formula: acceleration = BlackHoleBaseAcceleration * (1000 / distance)^2, capped at 10.0.
    /// Only applies within BlackHoleGravityMaxRange.
    /// </summary>
    private static void ProcessBlackHoleGravity(
        CombatShipSnapshot ship,
        CombatGridObstacle obstacle)
    {
        double distance = ship.Position.DistanceTo(obstacle.Position);

        if (distance > CombatConstants.BlackHoleGravityMaxRange) return;
        if (distance < 0.001) return;

        double rawAcceleration = CombatConstants.BlackHoleBaseAcceleration
            * Math.Pow(1000.0 / distance, 2);
        double acceleration = Math.Min(rawAcceleration, 10.0);

        ShipPhysics.ApplyGravity(ship, obstacle.Position, acceleration);
    }

    /// <summary>
    /// Deal heat damage to ships within close proximity of a star.
    /// Damage zone: star radius * StarHeatDamageRadiusMultiplier.
    /// Deals StarHeatDamagePerTick each tick the ship is in the zone.
    /// </summary>
    private static void ProcessStarHeat(
        CombatShipSnapshot ship,
        CombatGridObstacle obstacle,
        List<EnvironmentalDamageEvent> events)
    {
        double distance = ship.Position.DistanceTo(obstacle.Position);
        double heatRadius = obstacle.Radius * CombatConstants.StarHeatDamageRadiusMultiplier;

        if (distance > heatRadius) return;

        double damage = CombatConstants.StarHeatDamagePerTick;
        ship.CurrentStructurePoints -= (decimal)damage;

        bool destroyed = false;
        if (ship.CurrentStructurePoints <= 0)
        {
            ship.CurrentStructurePoints = 0;
            ship.IsAlive = false;
            destroyed = true;
        }

        events.Add(new EnvironmentalDamageEvent
        {
            ShipId = ship.ShipId,
            SourceType = "StarHeat",
            DamageDealt = damage,
            ShipDestroyed = destroyed,
            ShipPosition = ship.Position
        });
    }

    /// <summary>
    /// Check if the ship's position is inside an obstacle. If so, deal collision damage
    /// proportional to the ship's current speed.
    /// Damage = speed * CollisionDamageMultiplier.
    /// </summary>
    private static void ProcessCollision(
        CombatShipSnapshot ship,
        CombatGridObstacle obstacle,
        List<EnvironmentalDamageEvent> events)
    {
        double distance = ship.Position.DistanceTo(obstacle.Position);
        if (distance > obstacle.Radius) return;

        double speed = ship.Velocity.Length();
        if (speed < 0.001) return;

        double damage = speed * CombatConstants.CollisionDamageMultiplier;
        ship.CurrentStructurePoints -= (decimal)damage;

        bool destroyed = false;
        if (ship.CurrentStructurePoints <= 0)
        {
            ship.CurrentStructurePoints = 0;
            ship.IsAlive = false;
            destroyed = true;
        }

        events.Add(new EnvironmentalDamageEvent
        {
            ShipId = ship.ShipId,
            SourceType = $"Collision:{obstacle.Type}",
            DamageDealt = damage,
            ShipDestroyed = destroyed,
            ShipPosition = ship.Position
        });
    }
}

using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Newtonian physics for ship movement during combat simulation.
/// All methods are static -- no internal state. Mutates the passed CombatShipSnapshot directly.
/// </summary>
public static class ShipPhysics
{
    /// <summary>
    /// Apply thrust in a given direction. Sets the ship's acceleration vector
    /// from angle (radians) and power (0.0 to 1.0) scaled by effective acceleration.
    /// Effective acceleration is condition-weighted from engine modules, degrading as
    /// engines take damage. Also updates ship facing to the thrust direction.
    /// </summary>
    public static void ApplyThrust(CombatShipSnapshot ship, double angle, double power)
    {
        power = Math.Clamp(power, 0.0, 1.0);
        double magnitude = power * DamageApplicator.CalculateEffectiveAcceleration(ship);
        ship.Acceleration = new Vector2D(
            Math.Cos(angle) * magnitude,
            Math.Sin(angle) * magnitude
        );
        ship.Facing = angle;
    }

    /// <summary>
    /// Integrate physics for one simulation tick:
    /// velocity += acceleration, clamp speed to effective max speed, position += velocity.
    /// Effective max speed is condition-weighted from engine modules, so damaged engines
    /// reduce the ship's top speed (death-spiral effect).
    /// </summary>
    public static void UpdatePosition(CombatShipSnapshot ship)
    {
        // Apply acceleration to velocity
        ship.Velocity = new Vector2D(
            ship.Velocity.X + ship.Acceleration.X,
            ship.Velocity.Y + ship.Acceleration.Y
        );

        // Clamp speed to effective max speed (degrades with engine module damage)
        double effectiveMaxSpeed = DamageApplicator.CalculateEffectiveSpeed(ship);
        double speed = ship.Velocity.Length();
        if (speed > effectiveMaxSpeed && speed > 0)
        {
            double scale = effectiveMaxSpeed / speed;
            ship.Velocity = new Vector2D(
                ship.Velocity.X * scale,
                ship.Velocity.Y * scale
            );
        }

        // Apply velocity to position
        ship.Position = new Vector2D(
            ship.Position.X + ship.Velocity.X,
            ship.Position.Y + ship.Velocity.Y
        );

        // Update current speed
        ship.CurrentSpeed = ship.Velocity.Length();
    }

    /// <summary>
    /// Apply gravitational acceleration toward a gravity source.
    /// Adds the gravity vector directly to velocity (gravity is a continuous force).
    /// </summary>
    public static void ApplyGravity(CombatShipSnapshot ship, Vector2D gravitySource, double acceleration)
    {
        double dx = gravitySource.X - ship.Position.X;
        double dy = gravitySource.Y - ship.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.001) return; // Avoid division by zero

        double nx = dx / distance;
        double ny = dy / distance;

        ship.Velocity = new Vector2D(
            ship.Velocity.X + nx * acceleration,
            ship.Velocity.Y + ny * acceleration
        );
    }

    /// <summary>
    /// Zero out the ship's acceleration vector. Ship continues at current velocity (inertia).
    /// </summary>
    public static void StopThrust(CombatShipSnapshot ship)
    {
        ship.Acceleration = new Vector2D(0, 0);
    }

    /// <summary>
    /// Auto-navigate toward a target position at a desired speed.
    /// Calculates the angle from ship to target, applies thrust at the power level
    /// needed to reach desiredSpeed. If close to target, decelerates.
    /// Uses effective acceleration/speed (condition-weighted) so damaged engines
    /// affect navigation performance.
    /// </summary>
    public static void MoveToWaypoint(CombatShipSnapshot ship, Vector2D target, double desiredSpeed)
    {
        double dx = target.X - ship.Position.X;
        double dy = target.Y - ship.Position.Y;
        double distanceToTarget = Math.Sqrt(dx * dx + dy * dy);

        if (distanceToTarget < 1.0)
        {
            // Close enough -- stop thrust
            StopThrust(ship);
            return;
        }

        double angleToTarget = Math.Atan2(dy, dx);
        double currentSpeed = ship.Velocity.Length();
        double effectiveMaxSpeed = DamageApplicator.CalculateEffectiveSpeed(ship);

        // Use DamageApplicator.CalculateEffectiveAcceleration for consistency with ApplyThrust.
        double effectiveAcceleration = DamageApplicator.CalculateEffectiveAcceleration(ship);

        // Calculate stopping distance at current speed:
        // d = v^2 / (2 * a), where a is effective acceleration
        double stoppingDistance = (currentSpeed * currentSpeed) / (2.0 * effectiveAcceleration + 0.001);

        if (distanceToTarget <= stoppingDistance && currentSpeed > desiredSpeed * 0.5)
        {
            // Need to decelerate -- thrust opposite to velocity
            double velocityAngle = Math.Atan2(ship.Velocity.Y, ship.Velocity.X);
            double reverseAngle = velocityAngle + Math.PI;
            ApplyThrust(ship, reverseAngle, 1.0);
        }
        else
        {
            // Accelerate toward target
            double power = Math.Clamp(desiredSpeed / (effectiveMaxSpeed + 0.001), 0.0, 1.0);
            if (currentSpeed < desiredSpeed)
            {
                power = 1.0; // Full thrust until we reach desired speed
            }
            ApplyThrust(ship, angleToTarget, power);
        }
    }
}

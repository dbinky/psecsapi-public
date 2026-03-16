namespace psecsapi.Combat.Scripting;

/// <summary>
/// Utility functions exposed to combat scripts as the 'utils' object.
/// All methods accept IDictionary arguments (Jint delegates convert JsValue to these).
/// </summary>
public static class ScriptUtilities
{
    /// <summary>
    /// Euclidean distance between two position objects.
    /// JS signature: utils.distance({x, y}, {x, y}) => number
    /// </summary>
    public static double Distance(IDictionary<string, object> a, IDictionary<string, object> b)
    {
        double ax = Convert.ToDouble(a["x"]);
        double ay = Convert.ToDouble(a["y"]);
        double bx = Convert.ToDouble(b["x"]);
        double by = Convert.ToDouble(b["y"]);

        double dx = bx - ax;
        double dy = by - ay;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Angle from position a to position b in radians.
    /// JS signature: utils.angleTo({x, y}, {x, y}) => number (radians)
    /// </summary>
    public static double AngleTo(IDictionary<string, object> a, IDictionary<string, object> b)
    {
        double ax = Convert.ToDouble(a["x"]);
        double ay = Convert.ToDouble(a["y"]);
        double bx = Convert.ToDouble(b["x"]);
        double by = Convert.ToDouble(b["y"]);

        return Math.Atan2(by - ay, bx - ax);
    }

    /// <summary>
    /// Predicts the intercept point for a projectile fired from 'me' at 'enemy'
    /// given a projectile speed. Returns {x, y} of the predicted intercept position.
    /// Uses a first-order linear prediction based on enemy velocity.
    ///
    /// JS signature: utils.leadTarget(me, enemy, projectileSpeed) => {x, y}
    ///
    /// If the target cannot be intercepted (projectile too slow), returns the
    /// enemy's current position as a fallback.
    /// </summary>
    public static Dictionary<string, object> LeadTarget(
        IDictionary<string, object> me,
        IDictionary<string, object> enemy,
        double projectileSpeed)
    {
        var mePos = ExtractPosition(me);
        var enemyPos = ExtractPosition(enemy);
        var enemyVel = ExtractVelocity(enemy);

        double dx = enemyPos.x - mePos.x;
        double dy = enemyPos.y - mePos.y;

        if (projectileSpeed <= 0)
        {
            return new Dictionary<string, object> { ["x"] = enemyPos.x, ["y"] = enemyPos.y };
        }

        // Solve quadratic for intercept time:
        // |enemyPos + enemyVel * t - mePos|^2 = (projectileSpeed * t)^2
        double a = enemyVel.x * enemyVel.x + enemyVel.y * enemyVel.y
                   - projectileSpeed * projectileSpeed;
        double b = 2.0 * (dx * enemyVel.x + dy * enemyVel.y);
        double c = dx * dx + dy * dy;

        double discriminant = b * b - 4.0 * a * c;

        double interceptTime;

        if (Math.Abs(a) < 1e-10)
        {
            // Linear case: enemy speed equals projectile speed
            if (Math.Abs(b) < 1e-10)
            {
                return new Dictionary<string, object> { ["x"] = enemyPos.x, ["y"] = enemyPos.y };
            }
            interceptTime = -c / b;
        }
        else if (discriminant < 0)
        {
            return new Dictionary<string, object> { ["x"] = enemyPos.x, ["y"] = enemyPos.y };
        }
        else
        {
            double sqrtDisc = Math.Sqrt(discriminant);
            double t1 = (-b - sqrtDisc) / (2.0 * a);
            double t2 = (-b + sqrtDisc) / (2.0 * a);

            if (t1 > 0 && t2 > 0)
                interceptTime = Math.Min(t1, t2);
            else if (t1 > 0)
                interceptTime = t1;
            else if (t2 > 0)
                interceptTime = t2;
            else
                return new Dictionary<string, object> { ["x"] = enemyPos.x, ["y"] = enemyPos.y };
        }

        double predictedX = enemyPos.x + enemyVel.x * interceptTime;
        double predictedY = enemyPos.y + enemyVel.y * interceptTime;

        return new Dictionary<string, object> { ["x"] = predictedX, ["y"] = predictedY };
    }

    private static (double x, double y) ExtractPosition(IDictionary<string, object> obj)
    {
        if (obj.TryGetValue("position", out var posObj) && posObj is IDictionary<string, object> pos)
        {
            return (Convert.ToDouble(pos["x"]), Convert.ToDouble(pos["y"]));
        }
        return (Convert.ToDouble(obj["x"]), Convert.ToDouble(obj["y"]));
    }

    private static (double x, double y) ExtractVelocity(IDictionary<string, object> obj)
    {
        if (obj.TryGetValue("velocity", out var velObj) && velObj is IDictionary<string, object> vel)
        {
            return (Convert.ToDouble(vel["x"]), Convert.ToDouble(vel["y"]));
        }
        return (0.0, 0.0);
    }
}

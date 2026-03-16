namespace psecsapi.Combat.Scripting;

/// <summary>
/// Collects commands issued by a combat script during execution.
/// Exposed to JavaScript as the 'commands' object.
/// Each method appends a typed ScriptCommand to the internal list.
/// The simulation reads the collected commands after script execution.
/// </summary>
public class ScriptCommandCollector
{
    private readonly List<ScriptCommand> _commands = new();

    /// <summary>
    /// Returns all commands collected during the current execution and clears the list.
    /// </summary>
    public List<ScriptCommand> DrainCommands()
    {
        var result = new List<ScriptCommand>(_commands);
        _commands.Clear();
        return result;
    }

    /// <summary>
    /// Returns the commands collected so far without clearing.
    /// </summary>
    public IReadOnlyList<ScriptCommand> GetCommands() => _commands.AsReadOnly();

    /// <summary>
    /// Newtonian thrust: apply force in a direction at a given power level.
    /// </summary>
    /// <param name="angle">Direction in radians.</param>
    /// <param name="power">Power level 0.0 to 1.0.</param>
    public void Thrust(double angle, double power)
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.Thrust,
            Angle = angle,
            Power = Math.Clamp(power, 0.0, 1.0)
        });
    }

    /// <summary>
    /// Waypoint navigation: auto-navigate to a position at a desired speed.
    /// </summary>
    public void MoveTo(double x, double y, double speed)
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.MoveTo,
            X = x,
            Y = y,
            Speed = Math.Max(0.0, speed)
        });
    }

    /// <summary>
    /// Kill all thrust -- coast on current velocity.
    /// </summary>
    public void Stop()
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.Stop
        });
    }

    /// <summary>
    /// Fire a weapon at a specific enemy ship.
    /// </summary>
    /// <param name="weaponId">The weapon's string identifier.</param>
    /// <param name="targetId">The target ship's string identifier.</param>
    public void Fire(string weaponId, string targetId)
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.Fire,
            WeaponId = weaponId ?? string.Empty,
            TargetId = targetId ?? string.Empty
        });
    }

    /// <summary>
    /// Fire a weapon at specific coordinates (for leading shots).
    /// </summary>
    /// <param name="weaponId">The weapon's string identifier.</param>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate.</param>
    public void FireAt(string weaponId, double x, double y)
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.FireAt,
            WeaponId = weaponId ?? string.Empty,
            X = x,
            Y = y
        });
    }

    /// <summary>
    /// Disable auto-fire for this tick.
    /// </summary>
    public void HoldFire()
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.HoldFire
        });
    }

    /// <summary>
    /// Head for nearest grid boundary at maximum speed.
    /// </summary>
    public void Flee()
    {
        _commands.Add(new ScriptCommand
        {
            Type = ScriptCommandType.Flee
        });
    }
}

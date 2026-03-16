namespace psecsapi.Combat.Scripting;

/// <summary>
/// The default combat script used when a ship has no player script assigned,
/// or when the player's script is too large for the ship's compute capacity,
/// or when the player's script has a syntax error.
///
/// Behavior: flee toward the nearest grid boundary at maximum speed every tick.
/// </summary>
public static class DefaultFleeScript
{
    /// <summary>
    /// The JavaScript source for the default flee behavior.
    /// </summary>
    public const string Source = @"
function onTick(state) {
    commands.flee();
}
";
}

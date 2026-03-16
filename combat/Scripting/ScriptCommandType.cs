namespace psecsapi.Combat.Scripting;

/// <summary>
/// The type of command issued by a combat script.
/// </summary>
public enum ScriptCommandType
{
    Thrust,
    MoveTo,
    Stop,
    Fire,
    FireAt,
    HoldFire,
    Flee
}

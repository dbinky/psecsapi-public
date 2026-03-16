using Orleans;

namespace psecsapi.Combat.Scripting;

/// <summary>
/// A single command issued by a combat script during one tick's execution.
/// </summary>
[GenerateSerializer]
public class ScriptCommand
{
    [Id(0)]
    public ScriptCommandType Type { get; set; }

    /// <summary>Angle in radians for Thrust command.</summary>
    [Id(1)]
    public double Angle { get; set; }

    /// <summary>Power 0.0-1.0 for Thrust command, or desired speed for MoveTo.</summary>
    [Id(2)]
    public double Power { get; set; }

    /// <summary>Target X coordinate for MoveTo / FireAt.</summary>
    [Id(3)]
    public double X { get; set; }

    /// <summary>Target Y coordinate for MoveTo / FireAt.</summary>
    [Id(4)]
    public double Y { get; set; }

    /// <summary>Desired speed for MoveTo command.</summary>
    [Id(5)]
    public double Speed { get; set; }

    /// <summary>Weapon identifier for Fire / FireAt.</summary>
    [Id(6)]
    public string WeaponId { get; set; } = string.Empty;

    /// <summary>Target ship identifier for Fire.</summary>
    [Id(7)]
    public string TargetId { get; set; } = string.Empty;
}

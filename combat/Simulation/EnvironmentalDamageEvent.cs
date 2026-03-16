using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Result of an environmental damage event for recording in the combat event stream.
/// </summary>
public class EnvironmentalDamageEvent
{
    public Guid ShipId { get; init; }
    public string SourceType { get; init; } = string.Empty;
    public double DamageDealt { get; init; }
    public bool ShipDestroyed { get; init; }
    public Vector2D ShipPosition { get; init; } = Vector2D.Zero;
}

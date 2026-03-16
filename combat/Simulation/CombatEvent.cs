using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Base combat event for the simulation event stream.
/// Phase 11 will replace this with Protobuf-serialized event types.
/// For now, this captures the essential data for each event.
/// </summary>
public class CombatEvent
{
    public int Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid? ShipId { get; init; }
    public Guid? TargetShipId { get; init; }
    public Vector2D? Position { get; init; }
    public double? DamageDealt { get; init; }
    public string? Detail { get; init; }
}

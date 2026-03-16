using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Record of a destroyed ship.
/// </summary>
public class DestroyedShipRecord
{
    public Guid ShipId { get; init; }
    public Guid? DestroyerShipId { get; init; } // null if destroyed by environment
    public Vector2D LastPosition { get; init; } = Vector2D.Zero;
    public int DestroyedAtTick { get; init; }
}

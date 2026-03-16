using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Record of a ship that fled the combat grid.
/// </summary>
public class FledShipRecord
{
    public Guid ShipId { get; init; }
    public Vector2D ExitPosition { get; init; } = Vector2D.Zero;
    public int ExitTick { get; init; }
}

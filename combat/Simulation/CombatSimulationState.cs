using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// State object passed to ship scripts on compute ticks.
/// Provides the ship's view of the battlefield.
/// </summary>
public class CombatSimulationState
{
    public CombatShipSnapshot MyShip { get; init; } = null!;
    public List<CombatShipSnapshot> MyFleet { get; init; } = new();
    public List<CombatShipSnapshot> EnemyFleet { get; init; } = new();
    public CombatGrid Grid { get; init; } = null!;
    public int Tick { get; init; }
    public List<Projectile> Projectiles { get; init; } = new();
}

using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Complete result of a combat simulation run.
/// Contains the outcome, surviving/destroyed/fled ship data, and the raw event stream
/// for later Protobuf serialization (Phase 11).
/// </summary>
public class CombatSimulationResult
{
    /// <summary>
    /// The final outcome: AttackerWon, DefenderWon, Draw, or TimedOut.
    /// </summary>
    public CombatOutcome Outcome { get; init; }

    /// <summary>
    /// Total simulation ticks executed.
    /// </summary>
    public int TotalTicks { get; init; }

    /// <summary>
    /// Simulated duration in seconds (TotalTicks / ticksPerSecond).
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Attacker ships that survived (IsAlive == true, HasFled == false).
    /// </summary>
    public List<CombatShipSnapshot> SurvivingAttackerShips { get; init; } = new();

    /// <summary>
    /// Defender ships that survived (IsAlive == true, HasFled == false).
    /// </summary>
    public List<CombatShipSnapshot> SurvivingDefenderShips { get; init; } = new();

    /// <summary>
    /// Ships that were destroyed during combat.
    /// </summary>
    public List<DestroyedShipRecord> DestroyedShips { get; init; } = new();

    /// <summary>
    /// Ships that fled the combat grid.
    /// </summary>
    public List<FledShipRecord> FledShips { get; init; } = new();

    /// <summary>
    /// Chronological list of all combat events.
    /// </summary>
    public List<CombatEvent> Events { get; init; } = new();
}

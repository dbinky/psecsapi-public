using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Checks whether combat end conditions have been met.
/// A ship counts as "eliminated" if IsAlive == false OR HasFled == true.
/// </summary>
public static class CombatEndConditionChecker
{
    /// <summary>
    /// Evaluate whether combat should end this tick.
    /// Returns (ended: true, outcome) if a condition is met, or (ended: false, null) if combat continues.
    /// </summary>
    public static (bool Ended, CombatOutcome? Outcome) Check(
        List<CombatShipSnapshot> attackerShips,
        List<CombatShipSnapshot> defenderShips,
        int currentTick,
        int maxTicks)
    {
        bool allAttackersEliminated = attackerShips.Count == 0
            || attackerShips.All(s => !s.IsAlive || s.HasFled);
        bool allDefendersEliminated = defenderShips.Count == 0
            || defenderShips.All(s => !s.IsAlive || s.HasFled);

        if (allAttackersEliminated && allDefendersEliminated)
        {
            return (true, CombatOutcome.Draw);
        }

        if (allAttackersEliminated)
        {
            return (true, CombatOutcome.DefenderWon);
        }

        if (allDefendersEliminated)
        {
            return (true, CombatOutcome.AttackerWon);
        }

        if (currentTick >= maxTicks)
        {
            return (true, CombatOutcome.TimedOut);
        }

        return (false, null);
    }
}

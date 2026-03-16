using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public enum CombatOutcome
{
    AttackerWon,
    DefenderWon,
    Draw,
    TimedOut
}

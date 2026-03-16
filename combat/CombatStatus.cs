using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public enum CombatStatus
{
    InProgress,
    Completed,
    Failed
}

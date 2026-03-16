using Orleans;

namespace psecsapi.Combat;

/// <summary>
/// Records the result of a module hit during damage resolution.
/// Tracks which module was hit, condition before/after, and whether it was destroyed.
/// </summary>
[GenerateSerializer]
public class ModuleHitResult
{
    [Id(0)] public Guid ModuleId { get; set; }
    [Id(1)] public string ModuleName { get; set; } = string.Empty;
    [Id(2)] public double ConditionBefore { get; set; }
    [Id(3)] public double ConditionAfter { get; set; }
    [Id(4)] public double ConditionDamage { get; set; }
    [Id(5)] public bool WasDestroyed { get; set; }
}

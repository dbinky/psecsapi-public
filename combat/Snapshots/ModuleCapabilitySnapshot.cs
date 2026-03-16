using Orleans;
using psecsapi.Domain.Modules;

namespace psecsapi.Combat.Snapshots;

/// <summary>
/// A single capability entry within a ModuleSnapshot.
/// </summary>
[GenerateSerializer]
public record class ModuleCapabilitySnapshot
{
    [Id(0)] public ModuleCapabilityType CapabilityType { get; init; }

    [Id(1)] public decimal BaseValue { get; init; }
}

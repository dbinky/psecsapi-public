using Orleans;
using psecsapi.Domain.Modules;

namespace psecsapi.Combat.Snapshots;

/// <summary>
/// Frozen snapshot of a ship module for combat simulation.
/// Provides condition-adjusted capability values for the simulation engine.
/// </summary>
[GenerateSerializer]
public record class ModuleSnapshot
{
    /// <summary>Module instance ID from the TechModule.</summary>
    [Id(0)] public Guid ModuleId { get; init; }

    /// <summary>Module name (e.g., "PulseLaser", "PhaseShield", "IonDrive").</summary>
    [Id(1)] public string Name { get; init; } = string.Empty;

    /// <summary>True if this module occupies exterior slots, false for interior-only modules.</summary>
    [Id(2)] public bool IsExterior { get; init; }

    /// <summary>Current condition percentage (0-100). Modules at 0 are destroyed.</summary>
    [Id(3)] public decimal Condition { get; init; }

    /// <summary>List of capabilities this module provides, as (type, base value) pairs.</summary>
    [Id(4)] public List<ModuleCapabilitySnapshot> Capabilities { get; init; } = new();

    /// <summary>Power requirement for this module. Damaged modules still consume full power.</summary>
    [Id(5)] public double PowerRequired { get; init; }

    /// <summary>
    /// Returns the effective capability value, scaled by condition.
    /// A module at 50% condition provides 50% of its base capability.
    /// A destroyed module (0% condition) provides zero capability but still consumes requirements.
    /// </summary>
    public double GetEffectiveCapability(ModuleCapabilityType type)
    {
        var capability = Capabilities.FirstOrDefault(c => c.CapabilityType == type);
        if (capability == null)
            return 0.0;

        return (double)capability.BaseValue * ((double)Condition / 100.0);
    }
}

namespace psecsapi.api.models.Cargo;

/// <summary>
/// Response model for cargo asset inspection.
/// </summary>
public class CargoInspectResponseModel
{
    /// <summary>Boxed asset ID</summary>
    public Guid AssetId { get; set; }

    /// <summary>Asset type (Resource, Component, TechModule, Alloy)</summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>Asset name (resource name, component definition ID, module name, alloy definition ID)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Quantity in the boxed asset</summary>
    public decimal Quantity { get; set; }

    /// <summary>Total mass of the asset</summary>
    public decimal Mass { get; set; }

    /// <summary>Resource properties (OQ, PE, SR, etc.) — resources only</summary>
    public Dictionary<string, int>? ResourceProperties { get; set; }

    /// <summary>Raw resource ID — resources only</summary>
    public Guid? RawResourceId { get; set; }

    /// <summary>Quality properties (Integrity, Flexibility, etc.) — components only</summary>
    public Dictionary<string, decimal>? ComponentQualities { get; set; }

    /// <summary>Tech tier — components and modules</summary>
    public int? Tier { get; set; }

    /// <summary>Category — components and modules</summary>
    public string? Category { get; set; }

    /// <summary>Definition ID — components and modules</summary>
    public string? DefinitionId { get; set; }

    /// <summary>Slot type (Internal/External) — modules only</summary>
    public string? SlotType { get; set; }

    /// <summary>Module capabilities — modules only</summary>
    public List<ModuleCapabilityResponseModel>? ModuleCapabilities { get; set; }

    /// <summary>Module requirements — modules only</summary>
    public List<ModuleRequirementResponseModel>? ModuleRequirements { get; set; }

    /// <summary>Alloy computed properties — alloys only</summary>
    public Dictionary<string, int>? AlloyProperties { get; set; }
}

namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Response model for blueprint detail introspection.
/// </summary>
public class BlueprintDetailResponseModel
{
    /// <summary>Blueprint ID</summary>
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>Output type (component or module)</summary>
    public string OutputType { get; set; } = string.Empty;

    /// <summary>Output item ID</summary>
    public string OutputId { get; set; } = string.Empty;

    /// <summary>Base work units before quality adjustment</summary>
    public int BaseWorkUnits { get; set; }

    /// <summary>Resource inputs required</summary>
    public List<BlueprintInputResourceModel> InputResources { get; set; } = new();

    /// <summary>Component inputs required</summary>
    public List<BlueprintInputComponentModel> InputComponents { get; set; } = new();

    /// <summary>Output quality properties (for components)</summary>
    public Dictionary<string, decimal>? QualityProperties { get; set; }

    /// <summary>Output capabilities (for modules)</summary>
    public List<BlueprintCapabilityModel>? Capabilities { get; set; }
}

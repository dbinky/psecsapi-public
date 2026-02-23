namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Response model for a single owned blueprint instance.
/// </summary>
public class OwnedBlueprintModel
{
    /// <summary>Unique instance ID for this blueprint (used to start manufacturing jobs)</summary>
    public Guid InstanceId { get; set; }

    /// <summary>Blueprint definition ID from the tech tree</summary>
    public string BlueprintDefinitionId { get; set; } = string.Empty;

    /// <summary>Application ID that granted this blueprint</summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>Quality of this blueprint instance (affects manufacturing output)</summary>
    public decimal Quality { get; set; }

    /// <summary>When this blueprint was acquired</summary>
    public DateTime AcquiredAt { get; set; }

    /// <summary>Output type (Component or Module)</summary>
    public string OutputType { get; set; } = string.Empty;

    /// <summary>What this blueprint produces</summary>
    public string OutputName { get; set; } = string.Empty;
}

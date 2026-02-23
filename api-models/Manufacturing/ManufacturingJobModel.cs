namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Model representing a manufacturing job in status responses.
/// </summary>
public class ManufacturingJobModel
{
    /// <summary>Unique job ID</summary>
    public Guid JobId { get; set; }

    /// <summary>Ship where manufacturing is occurring</summary>
    public Guid ShipId { get; set; }

    /// <summary>Ship name for display</summary>
    public string ShipName { get; set; } = string.Empty;

    /// <summary>Blueprint being used</summary>
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>Blueprint quality</summary>
    public decimal BlueprintQuality { get; set; }

    /// <summary>Total items to manufacture</summary>
    public int TargetQuantity { get; set; }

    /// <summary>Items completed so far</summary>
    public int CompletedCount { get; set; }

    /// <summary>Progress on current item (0-100)</summary>
    public int CurrentItemProgressPercent { get; set; }

    /// <summary>Current job status</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Display name for output</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Human-readable name of the output item (e.g., "Culture Vat", "Duranium", "Fusion Reactor")</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Output type: "Component", "Module", or "Alloy"</summary>
    public string OutputType { get; set; } = string.Empty;

    /// <summary>Estimated completion time</summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>Whether auto-resume is enabled</summary>
    public bool AutoResume { get; set; }
}

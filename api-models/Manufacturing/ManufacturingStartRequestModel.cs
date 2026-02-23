using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Request model for starting a manufacturing job.
/// </summary>
public class ManufacturingStartRequestModel
{
    /// <summary>Ship to manufacture on</summary>
    [Required]
    public Guid ShipId { get; set; }

    /// <summary>Blueprint instance to use</summary>
    [Required]
    public Guid BlueprintInstanceId { get; set; }

    /// <summary>Number of items to manufacture (1 for modules)</summary>
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    /// <summary>Display name for the output</summary>
    public string? DisplayName { get; set; }

    /// <summary>Enable auto-resume when resources/space become available</summary>
    public bool AutoResume { get; set; }

    /// <summary>
    /// Explicit input selections. Key is the input label from blueprint,
    /// value is list of boxed asset IDs to use. Omit for auto-selection.
    /// </summary>
    public Dictionary<string, List<Guid>>? Inputs { get; set; }
}

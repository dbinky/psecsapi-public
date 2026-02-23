namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Capability output for module blueprint detail.
/// </summary>
public class BlueprintCapabilityModel
{
    public string Type { get; set; } = string.Empty;
    public decimal BaseValue { get; set; }
    public string? QualitySource { get; set; }
}

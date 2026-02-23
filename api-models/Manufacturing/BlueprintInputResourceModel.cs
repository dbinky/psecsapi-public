namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Resource input requirement for blueprint detail.
/// </summary>
public class BlueprintInputResourceModel
{
    public string Label { get; set; } = string.Empty;
    public string Qualifier { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Dictionary<string, string> PropertyMapping { get; set; } = new();

    /// <summary>
    /// Whether this input is a RawResource or Alloy. Defaults to RawResource if not specified.
    /// </summary>
    public string? InputKind { get; set; }

    /// <summary>
    /// For alloy inputs, the specific alloy definition ID required (e.g., "duranium", "volatite").
    /// </summary>
    public string? AlloyDefinitionId { get; set; }

    /// <summary>
    /// Input role for alloy recipes (Base, Additive, or Catalyst).
    /// </summary>
    public string? Role { get; set; }
}

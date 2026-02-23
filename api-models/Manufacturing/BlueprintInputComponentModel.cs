namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Component input requirement for blueprint detail.
/// </summary>
public class BlueprintInputComponentModel
{
    public string Label { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Dictionary<string, string> PropertyMapping { get; set; } = new();
}

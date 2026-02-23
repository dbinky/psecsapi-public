namespace psecsapi.api.models.Cargo;

/// <summary>
/// Quality properties for a single component instance.
/// </summary>
public class ComponentQualityModel
{
    public int Index { get; set; }
    public Dictionary<string, decimal> Properties { get; set; } = new();
}

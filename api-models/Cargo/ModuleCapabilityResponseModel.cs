namespace psecsapi.api.models.Cargo;

/// <summary>
/// A module capability with type and value.
/// </summary>
public class ModuleCapabilityResponseModel
{
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

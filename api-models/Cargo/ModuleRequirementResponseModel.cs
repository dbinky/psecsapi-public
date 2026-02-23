namespace psecsapi.api.models.Cargo;

/// <summary>
/// A module requirement with type and value.
/// </summary>
public class ModuleRequirementResponseModel
{
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

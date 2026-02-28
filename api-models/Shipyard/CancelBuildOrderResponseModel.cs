namespace psecsapi.api.models.Shipyard;

public class CancelBuildOrderResponseModel
{
    public bool Success { get; set; }
    public List<Guid> ReturnedAssetIds { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

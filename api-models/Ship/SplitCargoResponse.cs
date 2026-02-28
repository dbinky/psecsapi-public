namespace psecsapi.api.models.Ship;

public class SplitCargoResponse
{
    public bool Success { get; set; }
    public Guid? NewAssetId { get; set; }
    public string? ErrorMessage { get; set; }
}

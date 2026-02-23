namespace psecsapi.api.models.Ship;

public class CargoItemResponseModel
{
    public Guid AssetId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Mass { get; set; }
}

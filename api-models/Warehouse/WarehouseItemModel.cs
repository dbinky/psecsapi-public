namespace psecsapi.api.models.Warehouse;

public class WarehouseItemModel
{
    public Guid AssetId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal Mass { get; set; }
    public string Tier { get; set; } = string.Empty;
    public DateTime DepositedAt { get; set; }
    public decimal DailyRate { get; set; }
    public int PickupWindowDays { get; set; }
    public decimal DepositPaid { get; set; }
    public decimal DepositRemaining { get; set; }
    public DateTime NextBillingTime { get; set; }
    public DateTime? GracePeriodStart { get; set; }
}

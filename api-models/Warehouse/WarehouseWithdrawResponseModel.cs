namespace psecsapi.api.models.Warehouse;

public class WarehouseWithdrawResponseModel
{
    public bool Success { get; set; }
    public decimal CreditsRefunded { get; set; }
    public int ItemsPromoted { get; set; }
    public decimal PromotionRefund { get; set; }
    public string? ErrorMessage { get; set; }
}

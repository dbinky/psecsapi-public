namespace psecsapi.api.models.Warehouse;

public class WarehouseDepositResponseModel
{
    public bool Success { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal MassDeposited { get; set; }
    public decimal CreditsCharged { get; set; }
    public string? ErrorMessage { get; set; }
}

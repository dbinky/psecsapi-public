namespace psecsapi.api.models.Inventory;

public class WarehouseInventorySummaryModel
{
    public int TotalItems { get; set; }
    public decimal TotalMassStored { get; set; }
    public decimal FreeTierUsed { get; set; }
    public decimal PaidTierUsed { get; set; }
    public decimal FreeTierCapacity { get; set; }
}

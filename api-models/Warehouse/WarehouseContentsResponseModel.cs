namespace psecsapi.api.models.Warehouse;

public class WarehouseContentsResponseModel
{
    public List<WarehouseItemModel> Items { get; set; } = new();
    public decimal TotalMassStored { get; set; }
    public decimal FreeTierCapacity { get; set; }
    public decimal FreeTierUsed { get; set; }
    public decimal PaidTierUsed { get; set; }
}

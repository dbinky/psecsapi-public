namespace psecsapi.api.models.Warehouse;

public class WarehouseSummaryResponseModel
{
    public int TotalItems { get; set; }
    public decimal TotalMassStored { get; set; }
    public decimal FreeTierCapacity { get; set; }
    public decimal FreeTierUsed { get; set; }
    public decimal PaidTierUsed { get; set; }
    public decimal DailyBillingTotal { get; set; }
    public int ItemsInGracePeriod { get; set; }
}

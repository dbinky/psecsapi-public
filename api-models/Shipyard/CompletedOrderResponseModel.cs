namespace psecsapi.api.models.Shipyard;

public class CompletedOrderResponseModel
{
    public long OrderNumber { get; set; }
    public string ChassisName { get; set; } = "";
    public string CatalogId { get; set; } = "";
    public decimal BlueprintQuality { get; set; }
    public int InteriorSlots { get; set; }
    public int ExteriorSlots { get; set; }
    public int TotalSlots => InteriorSlots + ExteriorSlots;
    public Guid BoxedChassisId { get; set; }
    public DateTime CompletedTimestamp { get; set; }
}

namespace psecsapi.api.models.Shipyard;

public class ShipyardQueueEntryResponseModel
{
    public long OrderNumber { get; set; }
    public int TotalSlots { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal EstimatedMinutesRemaining { get; set; }
    public bool IsOwnOrder { get; set; }
    public DateTime PlacedTimestamp { get; set; }
}

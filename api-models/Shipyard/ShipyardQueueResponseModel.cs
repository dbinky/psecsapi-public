namespace psecsapi.api.models.Shipyard;

public class ShipyardQueueResponseModel
{
    public ShipyardQueueEntryResponseModel? CurrentBuild { get; set; }
    public List<ShipyardQueueEntryResponseModel> QueuedBuilds { get; set; } = new();
    public int TotalQueueDepth { get; set; }
}

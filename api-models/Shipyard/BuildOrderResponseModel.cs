namespace psecsapi.api.models.Shipyard;

public class BuildOrderResponseModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long OrderNumber { get; set; }
    public decimal TotalWorkUnits { get; set; }
    public int QueuePosition { get; set; }
    public decimal EstimatedMinutes { get; set; }
    public decimal BuildFee { get; set; }
}

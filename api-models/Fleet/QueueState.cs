namespace psecsapi.api.models.Fleet;

public class QueueState
{
    public Guid ConduitId { get; set; }
    public int QueueWidth { get; set; }
    public int QueueLength { get; set; }
    public int QueuePosition { get; set; }
    public DateTime EnqueuedTimestamp { get; set; }
}

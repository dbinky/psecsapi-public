namespace psecsapi.api.models.Events
{
    public class CloudEventsResponse
    {
        public List<CloudEventModel> Events { get; set; } = new();
        public string? Cursor { get; set; }
    }
}

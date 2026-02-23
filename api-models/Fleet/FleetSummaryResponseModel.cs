namespace psecsapi.api.models.Fleet
{
    public class FleetSummaryResponseModel
    {
        public Guid FleetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ShipCount { get; set; }
    }
}

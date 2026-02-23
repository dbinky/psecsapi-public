namespace psecsapi.api.models.Fleet
{
    public class FleetScanResultResponseModel
    {
        public Guid FleetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ShipCount { get; set; }
        public Dictionary<string, int>? ShipsByClass { get; set; }
        public decimal? TotalMass { get; set; }
        public List<ScannedShipResponseModel>? Ships { get; set; }
    }
}

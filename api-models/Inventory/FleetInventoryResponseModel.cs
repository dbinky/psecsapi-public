namespace psecsapi.api.models.Inventory
{
    public class FleetInventoryResponseModel
    {
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public List<ResourceTotalModel> Totals { get; set; } = new();
        public List<ShipSummaryModel> Ships { get; set; } = new();
        public DateTime SnapshotTime { get; set; }
    }
}

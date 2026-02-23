namespace psecsapi.api.models.Inventory
{
    public class CorpInventoryResponseModel
    {
        public List<ResourceTotalModel> Totals { get; set; } = new();
        public List<FleetSummaryModel> Fleets { get; set; } = new();
        public DateTime SnapshotTime { get; set; }
    }
}

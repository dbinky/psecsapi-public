namespace psecsapi.Console.Commands.Inventory
{
    public class CorpInventoryResponse
    {
        public List<ResourceTotalResponse> Totals { get; set; } = new();
        public List<FleetSummaryResponse> Fleets { get; set; } = new();
        public DateTime SnapshotTime { get; set; }
    }
}

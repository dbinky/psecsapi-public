namespace psecsapi.Console.Commands.Inventory
{
    public class FleetInventoryResponse
    {
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public List<ResourceTotalResponse> Totals { get; set; } = new();
        public List<ShipSummaryResponse> Ships { get; set; } = new();
        public DateTime SnapshotTime { get; set; }
    }
}

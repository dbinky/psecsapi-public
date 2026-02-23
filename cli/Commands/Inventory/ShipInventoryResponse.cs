namespace psecsapi.Console.Commands.Inventory
{
    public class ShipInventoryResponse
    {
        public Guid ShipId { get; set; }
        public string ShipName { get; set; } = string.Empty;
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public List<CargoHoldResponse> CargoHolds { get; set; } = new();
        public DateTime SnapshotTime { get; set; }
        public bool HasCargoDetails { get; set; }
    }
}

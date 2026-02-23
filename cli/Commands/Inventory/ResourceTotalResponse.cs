namespace psecsapi.Console.Commands.Inventory
{
    public class ResourceTotalResponse
    {
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceClass { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public List<FleetQuantityResponse> ByFleet { get; set; } = new();
    }
}

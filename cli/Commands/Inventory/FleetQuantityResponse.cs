namespace psecsapi.Console.Commands.Inventory
{
    public class FleetQuantityResponse
    {
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}

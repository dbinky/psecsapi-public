namespace psecsapi.api.models.Inventory
{
    public class FleetQuantityModel
    {
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}

namespace psecsapi.api.models.Inventory
{
    public class ResourceTotalModel
    {
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceClass { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public List<FleetQuantityModel> ByFleet { get; set; } = new();
    }
}

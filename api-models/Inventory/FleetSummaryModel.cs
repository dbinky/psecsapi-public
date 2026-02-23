namespace psecsapi.api.models.Inventory
{
    public class FleetSummaryModel
    {
        public Guid FleetId { get; set; }
        public string FleetName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public int ResourceTypeCount { get; set; }
    }
}

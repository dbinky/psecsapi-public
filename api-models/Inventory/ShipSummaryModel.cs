namespace psecsapi.api.models.Inventory
{
    public class ShipSummaryModel
    {
        public Guid ShipId { get; set; }
        public string ShipName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public int ResourceTypeCount { get; set; }
        public bool HasActiveExtraction { get; set; }
    }
}

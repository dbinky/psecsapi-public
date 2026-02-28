namespace psecsapi.Console.Commands.Inventory
{
    public class CargoItemResponse
    {
        public Guid BoxedResourceId { get; set; }
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceClass { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string AssetType { get; set; } = "Resource";
    }
}

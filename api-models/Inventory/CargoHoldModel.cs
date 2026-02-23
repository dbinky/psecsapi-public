namespace psecsapi.api.models.Inventory
{
    public class CargoHoldModel
    {
        public Guid CargoModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal Used { get; set; }
        public decimal Available => Capacity - Used;
        public List<CargoItemModel> Contents { get; set; } = new();
    }
}

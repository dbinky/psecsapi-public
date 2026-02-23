namespace psecsapi.Console.Commands.Inventory
{
    public class CargoHoldResponse
    {
        public Guid CargoModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public decimal Used { get; set; }
        public decimal Available => Capacity - Used;
        public List<CargoItemResponse> Contents { get; set; } = new();
    }
}

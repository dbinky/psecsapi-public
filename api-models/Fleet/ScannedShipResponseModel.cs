namespace psecsapi.api.models.Fleet
{
    public class ScannedShipResponseModel
    {
        public Guid ShipId { get; set; }
        public string Class { get; set; } = string.Empty;
        public decimal Mass { get; set; }
        public List<ScannedModuleResponseModel>? ExternalModules { get; set; }
        public List<ScannedModuleResponseModel>? InternalModules { get; set; }
    }
}

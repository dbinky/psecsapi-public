using psecsapi.api.models.Space;

namespace psecsapi.api.models.Fleet
{
    public class BasicSectorScanResponseModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreateTimestamp { get; set; }
        public string? SpawnedByUserId { get; set; }
        public List<ConduitResponseModel> Conduits { get; set; } = new List<ConduitResponseModel>();
        public Dictionary<int, string>? Orbitals { get; set; }
    }
}

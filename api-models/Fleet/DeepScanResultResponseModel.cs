namespace psecsapi.api.models.Fleet
{
    public class DeepScanResultResponseModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Order { get; set; } = string.Empty;
        public Dictionary<string, string>? PropertyAssessments { get; set; }
        public Dictionary<string, int>? PropertyValues { get; set; }
    }
}

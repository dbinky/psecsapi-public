namespace psecsapi.api.models.Research
{
    public class CompletedApplicationModel
    {
        public Guid InstanceId { get; set; }
        public string ApplicationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quality { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}

namespace psecsapi.api.models.Extraction
{
    public class ExtractionJobStatusResponseModel
    {
        public Guid JobId { get; set; }
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public decimal RatePerMinute { get; set; }
        public decimal? QuantityLimit { get; set; }
        public DateTime StartTime { get; set; }
        public decimal AccumulatedQuantity { get; set; }
    }
}

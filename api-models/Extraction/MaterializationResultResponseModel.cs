namespace psecsapi.api.models.Extraction
{
    public class MaterializationResultResponseModel
    {
        public Guid JobId { get; set; }
        public Guid BoxedResourceId { get; set; }
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public decimal MaterializedQuantity { get; set; }
    }
}

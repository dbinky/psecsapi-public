namespace psecsapi.api.models.Research
{
    public class ResearchAllocateRequestModel
    {
        public string TargetId { get; set; } = string.Empty;

        public int Percent { get; set; }
    }
}

namespace psecsapi.api.models.Research
{
    public class ResearchAllocateResponseModel
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public ActiveProjectModel? Project { get; set; }
    }
}

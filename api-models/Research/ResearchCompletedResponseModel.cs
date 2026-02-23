namespace psecsapi.api.models.Research
{
    public class ResearchCompletedResponseModel
    {
        public List<string> Technologies { get; set; } = new();
        public List<CompletedApplicationModel> Applications { get; set; } = new();
    }
}

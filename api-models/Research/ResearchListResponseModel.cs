namespace psecsapi.api.models.Research
{
    public class ResearchListResponseModel
    {
        public List<TechnologyListItem> Technologies { get; set; } = new();
        public List<ApplicationListItem> Applications { get; set; } = new();
    }
}

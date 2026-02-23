namespace psecsapi.api.models.Research
{
    public class TechnologyListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Tier { get; set; }
        public string PrimaryDiscipline { get; set; } = string.Empty;
        public string SecondaryDiscipline { get; set; } = string.Empty;
        public int ResearchCost { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public bool IsResearched { get; set; }
        public bool IsVisible { get; set; }
    }
}

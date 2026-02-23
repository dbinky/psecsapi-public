namespace psecsapi.api.models.Research
{
    public class ApplicationListItem
    {
        public string Id { get; set; } = string.Empty;
        public string TechnologyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int ResearchCost { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public bool IsResearched { get; set; }
        public bool IsVisible { get; set; }
        public int InstanceCount { get; set; }

        /// <summary>Modifier details (null if Type is not "modifier")</summary>
        public ModifierSummary? Modifier { get; set; }

        /// <summary>Blueprint output summary (null if Type is not "blueprint")</summary>
        public BlueprintSummary? Blueprint { get; set; }
    }
}

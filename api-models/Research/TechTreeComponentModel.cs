namespace psecsapi.api.models.Research;

public class TechTreeComponentModel
{
    public string ComponentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Tier { get; set; }
    public decimal Mass { get; set; }
    public List<string> QualityProperties { get; set; } = new();
}

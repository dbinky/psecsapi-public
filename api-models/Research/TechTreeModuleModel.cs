namespace psecsapi.api.models.Research;

public class TechTreeModuleModel
{
    public string ModuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Tier { get; set; }
    public decimal Mass { get; set; }
    public string SlotType { get; set; } = string.Empty;
    public int InteriorSlotsRequired { get; set; }
    public int ExteriorSlotsRequired { get; set; }
    public List<TechTreeModuleCapabilityModel> BaseCapabilities { get; set; } = new();
    public List<TechTreeModuleRequirementModel> Requirements { get; set; } = new();
}

public class TechTreeModuleCapabilityModel
{
    public string Type { get; set; } = string.Empty;
    public decimal BaseValue { get; set; }
}

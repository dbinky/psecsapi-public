namespace psecsapi.api.models.Ship;

public class ModuleInstallResponseModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> InstalledModuleNames { get; set; } = new();
    public int InteriorSlotsUsed { get; set; }
    public int ExteriorSlotsUsed { get; set; }
    public int InteriorSlotsAvailable { get; set; }
    public int ExteriorSlotsAvailable { get; set; }
}

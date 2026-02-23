namespace psecsapi.api.models.Ship;

public class ModuleUninstallResponseModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> UninstalledModuleNames { get; set; } = new();
    public List<Guid> BoxedModuleIds { get; set; } = new();
}

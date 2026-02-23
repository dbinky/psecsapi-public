using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Ship;

public class UninstallModulesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(80)]
    public List<Guid> ModuleIds { get; set; } = new();

    [Required]
    public Guid CargoModuleId { get; set; }
}

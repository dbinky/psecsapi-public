using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Ship;

public class InstallModulesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(80)]
    public List<Guid> BoxedModuleIds { get; set; } = new();
}

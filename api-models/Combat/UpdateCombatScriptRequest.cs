using System.ComponentModel.DataAnnotations;
using psecsapi.Domain.Combat;

namespace psecsapi.api.models.Combat
{
    public class UpdateCombatScriptRequest
    {
        [Required]
        [MaxLength(CombatConstants.MaxScriptNameLength)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(CombatConstants.MaxScriptSourceBytes)]
        public string Source { get; set; } = string.Empty;
    }
}

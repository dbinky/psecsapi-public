using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Fleet
{
    public class AssignCombatScriptRequest
    {
        [Required]
        public Guid ScriptId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Request to initiate fleet-vs-fleet combat.
    /// </summary>
    public class EngageCombatRequest
    {
        /// <summary>The attacking fleet's ID. Must belong to the authenticated user's corp.</summary>
        [Required]
        public Guid AttackerFleetId { get; set; }

        /// <summary>The target fleet's ID. Must be in the same sector as the attacker.</summary>
        [Required]
        public Guid TargetFleetId { get; set; }
    }
}

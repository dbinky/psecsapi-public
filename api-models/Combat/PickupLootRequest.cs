using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Request to pick up items from a loot field using a fleet's ship.
    /// </summary>
    public class PickupLootRequest
    {
        /// <summary>Fleet ID that is in the same sector as the loot field.</summary>
        [Required]
        public Guid FleetId { get; set; }

        /// <summary>Ship ID within the fleet that will receive the cargo.</summary>
        [Required]
        public Guid ShipId { get; set; }
    }
}

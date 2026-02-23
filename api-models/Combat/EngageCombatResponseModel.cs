namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Response from a combat engagement request. Combat runs asynchronously;
    /// use the CombatId to poll status or listen for corp events.
    /// </summary>
    public class EngageCombatResponseModel
    {
        /// <summary>Whether the engagement was successfully initiated.</summary>
        public bool Success { get; set; }

        /// <summary>The combat instance ID. Null if engagement failed.</summary>
        public Guid? CombatId { get; set; }

        /// <summary>Error message if engagement failed. Null on success.</summary>
        public string? ErrorMessage { get; set; }
    }
}

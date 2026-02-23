namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Current status of a combat instance.
    /// </summary>
    public class CombatStatusResponseModel
    {
        /// <summary>The combat instance ID.</summary>
        public Guid CombatId { get; set; }

        /// <summary>Current status: "InProgress", "Completed".</summary>
        public string Status { get; set; } = string.Empty;
    }
}

namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Full summary of a completed combat instance. Persists indefinitely even after replay data expires.
    /// </summary>
    public class CombatSummaryResponseModel
    {
        /// <summary>The combat instance ID.</summary>
        public Guid CombatId { get; set; }

        /// <summary>The attacking corp's ID.</summary>
        public Guid AttackerCorpId { get; set; }

        /// <summary>The defending corp's ID.</summary>
        public Guid DefenderCorpId { get; set; }

        /// <summary>The attacking fleet's ID.</summary>
        public Guid AttackerFleetId { get; set; }

        /// <summary>The defending fleet's ID.</summary>
        public Guid DefenderFleetId { get; set; }

        /// <summary>Outcome: "AttackerWon", "DefenderWon", "Draw", "TimedOut".</summary>
        public string Outcome { get; set; } = string.Empty;

        /// <summary>Total simulation ticks elapsed.</summary>
        public int DurationTicks { get; set; }

        /// <summary>Simulated duration in seconds (ticks / tick rate).</summary>
        public double DurationSeconds { get; set; }

        /// <summary>IDs of ships destroyed during combat.</summary>
        public List<string> ShipsDestroyed { get; set; } = new();

        /// <summary>IDs of ships that fled the combat grid.</summary>
        public List<string> ShipsFled { get; set; } = new();

        /// <summary>When the combat occurred (UTC).</summary>
        public DateTime Timestamp { get; set; }
    }
}

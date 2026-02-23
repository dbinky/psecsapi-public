namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// A single entry in a corp's combat history. Lightweight summary for list display.
    /// </summary>
    public class CombatHistoryItemModel
    {
        /// <summary>The combat instance ID. Use to fetch full summary or replay.</summary>
        public Guid CombatId { get; set; }

        /// <summary>The opponent corp's ID.</summary>
        public Guid OpponentCorpId { get; set; }

        /// <summary>The opponent corp's display name at the time of combat.</summary>
        public string OpponentCorpName { get; set; } = string.Empty;

        /// <summary>Outcome from this corp's perspective: "Won", "Lost", "Draw", "TimedOut".</summary>
        public string Outcome { get; set; } = string.Empty;

        /// <summary>When the combat occurred (UTC).</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Number of this corp's ships lost in the combat.</summary>
        public int ShipLosses { get; set; }

        /// <summary>Number of enemy ships destroyed by this corp.</summary>
        public int ShipKills { get; set; }
    }
}

namespace psecsapi.api.models.Combat
{
    /// <summary>
    /// Paginated combat history for a corporation.
    /// </summary>
    public class CombatHistoryResponseModel
    {
        /// <summary>Combat history entries for the current page.</summary>
        public List<CombatHistoryItemModel> Items { get; set; } = new();

        /// <summary>Total number of combat history entries for this corp.</summary>
        public int TotalCount { get; set; }

        /// <summary>Current page number (1-based).</summary>
        public int Page { get; set; }

        /// <summary>Number of items per page.</summary>
        public int PageSize { get; set; }
    }
}

namespace psecsapi.Console.Commands.Catalog
{
    public class CatalogResourceEntryResponse
    {
        // Identity
        public Guid EntryId { get; set; }
        public Guid RawResourceId { get; set; }

        // Resource taxonomy (snapshot)
        public string Name { get; set; } = string.Empty;
        public string ShortNameKey { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Order { get; set; } = string.Empty;

        // Resource stats (snapshot)
        public Dictionary<string, int?> Properties { get; set; } = new();
        public decimal Density { get; set; }

        // Location (snapshot)
        public Guid SectorId { get; set; }
        public string SectorName { get; set; } = string.Empty;
        public int? OrbitalPosition { get; set; }

        // Discovery metadata
        public DateTime DiscoveredAt { get; set; }
        public string DiscoveredByUserId { get; set; } = string.Empty;

        // User data
        public bool IsFavorite { get; set; }
        public string? Note { get; set; }
    }
}

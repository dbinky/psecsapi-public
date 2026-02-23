using psecsapi.Grains.Interfaces.Resources.Models;

namespace psecsapi.api.models.Catalog
{
    public class ResourceCatalogEntryResponseModel
    {
        // Identity
        public Guid EntryId { get; set; }
        public Guid RawResourceId { get; set; }

        // Resource taxonomy (snapshot)
        public string Name { get; set; } = string.Empty;
        public string ShortNameKey { get; set; } = string.Empty;
        public RawResourceGroup Group { get; set; }
        public RawResourceType Type { get; set; }
        public RawResourceClass Class { get; set; }
        public RawResourceOrder Order { get; set; }

        // Resource stats (snapshot)
        public Dictionary<RawResourceProperty, int?> Properties { get; set; } = new();
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

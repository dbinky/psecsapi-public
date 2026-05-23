using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.api.models.User;

public class UserMapSectorModel
{
    public Guid EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SectorType Type { get; set; }
    public List<UserMapConduitModel> Conduits { get; set; } = new();
    public Dictionary<int, string>? Orbitals { get; set; }
    public DateTime CreateTimestamp { get; set; }
    public DateTime LastMappedTimestamp { get; set; }
    public string? SpawnedByUserId { get; set; }
    public bool IsFavorited { get; set; }
    public string? Note { get; set; }
    public DateTime? NoteTimestamp { get; set; }
}

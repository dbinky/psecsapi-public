using psecsapi.Grains.Interfaces.Space.Models;

namespace psecsapi.api.models.User
{
    public class UserMapSectorModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public SectorType Type { get; set; }
        public List<UserMapConduitModel> Conduits { get; set; } = new();
        public DateTime CreateTimestamp { get; set; }
        public DateTime LastMappedTimestamp { get; set; }
        public string? SpawnedByUserId { get; set; }
        public bool IsFavorited { get; set; }
        public string? Note { get; set; }
        public DateTime? NoteTimestamp { get; set; }
    }

    public class SetNoteRequestModel
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UserMapConduitModel
    {
        public Guid EntityId { get; set; }
        public int Length { get; set; }
        public int Width { get; set; }
    }
}

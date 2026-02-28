using psecsapi.Grains.Interfaces.Base.OwnedEntities;

namespace psecsapi.api.Models.Corp
{
    [Serializable]
    public class CorpProfileResponseModel
    {
        public Guid EntityId { get; init; }
        public string Name { get; init; } = string.Empty;
        public Dictionary<string, OwnedEntityAccessLevel>? AccessLevels { get; set; } = new();
        public decimal? Credits { get; set; }
        public DateTime? LastUpdateTimestamp { get; set; }
        public DateTime CreateTimestamp { get; set; }
    }
}

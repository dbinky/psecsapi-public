using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Space
{
    public class SpaceSectorResponseModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime? CreateTimestamp { get; set; }
        public string? SpawnedByUserId { get; set; }
        public NebulaSectorResponseModel? NebulaDetails { get; set; }
        public RubbleSectorResponseModel? RubbleDetails { get; set; }
        public StarSystemSectorResponseModel? StarSystemDetails { get; set; }
        public BlackHoleSectorResponseModel? BlackHoleDetails { get; set; }
    }
}

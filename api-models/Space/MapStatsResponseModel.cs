using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Space
{
    public class MapStatsResponseModel
    {
        public int SectorCount { get; set; }
        public int VoidSectorCount { get; set; }
        public int NebulaSectorCount { get; set; }
        public int RubbleSectorCount { get; set; }
        public int StarSystemSectorCount { get; set; }
        public int BlackHoleSectorCount { get; set; }
        public int NexusSectorCount { get; set; }
    }

    public class EnhancedMapStatsResponseModel
    {
        public GlobalMapStats Global { get; set; } = new();
        public PersonalMapStats? Personal { get; set; }
    }

    public class GlobalMapStats
    {
        public int TotalSectors { get; set; }
        public Dictionary<string, int> SectorsByType { get; set; } = new();
    }

    public class PersonalMapStats
    {
        public int TotalKnown { get; set; }
        public Dictionary<string, int> SectorsByType { get; set; } = new();
        public int Favorites { get; set; }
    }
}

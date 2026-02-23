using psecsapi.api.models.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Fleet
{
    public class FleetScanResponseModel
    {
        public SpaceSectorResponseModel? Sector { get; set; }
        public List<ConduitResponseModel> Conduits { get; set; } = new List<ConduitResponseModel>();
    }
}

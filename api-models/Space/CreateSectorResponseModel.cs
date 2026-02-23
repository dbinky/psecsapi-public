using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Space
{
    public class CreateSectorResponseModel
    {
        public List<SpaceSectorResponseModel> Sectors { get; set; } = new();
    }
}

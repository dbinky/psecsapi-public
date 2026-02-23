using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Space
{
    public class ConduitResponseModel
    {
        public Guid EntityId { get; set; }
        public Guid? OriginSectorId { get; set; }
        public Guid? EndpointSectorId { get; set; }
        public int? Width { get; set; }
        public int? Length { get; set; }
    }
}

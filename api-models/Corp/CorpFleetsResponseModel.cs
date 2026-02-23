using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Corp
{
    public class CorpFleetsResponseModel
    {
        public List<Guid> CorpFleets { get; set; } = new();
    }
}

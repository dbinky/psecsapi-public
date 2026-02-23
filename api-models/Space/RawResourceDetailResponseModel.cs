using psecsapi.Grains.Interfaces.Resources.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Space
{
    public class RawResourceDetailResponseModel
    {
        public string Name { get; set; } = string.Empty;
        public string ShortNameKey { get; set; } = string.Empty;
        public RawResourceGroup ResourceGroup { get; set; }
        public RawResourceType ResourceType { get; set; }
        public RawResourceClass ResourceClass { get; set; }
        public RawResourceOrder ResourceOrder { get; set; }
        public Dictionary<RawResourceProperty, int?> RawResourceProperties { get; set; } = [];
    }
}

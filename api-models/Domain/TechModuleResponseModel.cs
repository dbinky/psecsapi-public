using psecsapi.Domain.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Domain
{
    public class TechModuleResponseModel
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Tier { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SlotType { get; set; } = string.Empty;
        public List<ModuleCompatibility> Compatibilities { get; set; } = new();
        public int InteriorSlotsRequired { get; set; }
        public int ExteriorSlotsRequired { get; set; }
        public decimal Mass { get; set; }
        public bool IsEnabled { get; set; } = true;
        public decimal Condition { get; set; } = 100m;
        public List<ModuleCapabilityDetailResponseModel> Capabilities { get; set; } = new();
        public List<ModuleRequirementDetailResponseModel> Requirements { get; set; } = new();
    }
}

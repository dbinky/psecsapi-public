using psecsapi.api.models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psecsapi.api.models.Ship
{
    public class ShipDetailResponseModel
    {
        public Guid EntityId { get; set; }
        public Guid OwnerCorpId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AssetImageUrl { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public decimal? CurrentStructurePoints { get; set; }
        public decimal? CurrentHullPoints { get; set; }
        public DateTime LastUpdateTimestamp { get; set; }
        public DateTime CreateTimestamp { get; set; }
        public Guid FleetId { get; set; }
        public List<TechModuleResponseModel>? Modules { get; set; } = [];
        public List<ModuleCapabilityDetailResponseModel>? Capabilities { get; set; } = [];
        public List<ModuleRequirementDetailResponseModel>? Requirements { get; set; } = [];
        public bool? RequirementsMet { get; set; }
        public int TotalInteriorSlots { get; set; }
        public int TotalExteriorSlots { get; set; }
        public decimal MaxStructurePoints { get; set; }
        public decimal MaxHullPoints { get; set; }
        public decimal ShipMass { get; set; }
    }
}

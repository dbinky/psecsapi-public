using psecsapi.api.models.Domain;

namespace psecsapi.api.models.Fleet
{
    public class ScannedModuleResponseModel
    {
        public string Name { get; set; } = string.Empty;
        public List<ModuleCapabilityDetailResponseModel> Capabilities { get; set; } = new List<ModuleCapabilityDetailResponseModel>();
        public int InteriorSlotsRequired { get; set; }
        public int ExteriorSlotsRequired { get; set; }
    }
}

using System;

namespace psecsapi.api.models.Domain
{
    public class ModuleCapabilityDetailResponseModel
    {
        public string CapabilityType { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}

using System;

namespace psecsapi.api.models.Domain
{
    public class ModuleRequirementDetailResponseModel
    {
        public string RequirementType { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}

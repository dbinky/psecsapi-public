namespace psecsapi.api.models.Inventory
{
    public class CargoItemModel
    {
        public Guid BoxedResourceId { get; set; }
        public Guid RawResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceClass { get; set; } = string.Empty;
        public decimal Quantity { get; set; }

        /// <summary>Asset type: "Resource" or "Alloy".</summary>
        public string AssetType { get; set; } = "Resource";

        /// <summary>For alloy items: the alloy definition ID.</summary>
        public string? AlloyDefinitionId { get; set; }

        /// <summary>For alloy items: the alloy display name.</summary>
        public string? AlloyName { get; set; }

        /// <summary>For alloy items: computed property values keyed by property code.</summary>
        public Dictionary<string, int>? ComputedProperties { get; set; }
    }
}

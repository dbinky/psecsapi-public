namespace psecsapi.api.models.Ship;

public class ShipCatalogEntryResponseModel
{
    public string CatalogId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Class { get; set; } = "";
    public int InteriorSlots { get; set; }
    public int ExteriorSlots { get; set; }
    public int TotalSlots { get; set; }
    public decimal BaseStructurePoints { get; set; }
    public decimal BaseHullPoints { get; set; }
    public decimal BaseMass { get; set; }
}

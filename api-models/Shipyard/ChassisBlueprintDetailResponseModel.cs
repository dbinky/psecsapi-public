namespace psecsapi.api.models.Shipyard;

public class ChassisBlueprintDetailResponseModel
{
    public string BlueprintId { get; set; } = "";
    public string ChassisClass { get; set; } = "";
    public int BaseWorkUnitsPerSlot { get; set; }
    public List<ChassisBlueprintInputModel> BaseInputResources { get; set; } = new();
    public List<ChassisBlueprintComponentInputModel> BaseInputComponents { get; set; } = new();
    public List<ChassisBlueprintInputModel> PerInteriorSlotInputResources { get; set; } = new();
    public List<ChassisBlueprintComponentInputModel> PerInteriorSlotInputComponents { get; set; } = new();
    public List<ChassisBlueprintInputModel> PerExteriorSlotInputResources { get; set; } = new();
    public List<ChassisBlueprintComponentInputModel> PerExteriorSlotInputComponents { get; set; } = new();
    public List<ChassisBlueprintInputModel>? CalculatedTotalResources { get; set; }
    public List<ChassisBlueprintComponentInputModel>? CalculatedTotalComponents { get; set; }
    public decimal? CalculatedTotalWorkUnits { get; set; }
}

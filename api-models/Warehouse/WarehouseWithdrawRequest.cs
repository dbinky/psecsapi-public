using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Warehouse;

public class WarehouseWithdrawRequest
{
    [Required]
    public Guid AssetId { get; set; }

    [Required]
    public Guid FleetId { get; set; }

    [Required]
    public Guid ShipId { get; set; }

    [Required]
    public Guid CargoModuleId { get; set; }
}

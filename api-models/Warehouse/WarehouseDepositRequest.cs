using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Warehouse;

public class WarehouseDepositRequest
{
    [Required]
    public Guid AssetId { get; set; }

    [Required]
    public string AssetType { get; set; } = string.Empty;

    [Required]
    public Guid FleetId { get; set; }

    [Required]
    public Guid ShipId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PickupWindowDays must be at least 1 when specified.")]
    public int? PickupWindowDays { get; set; }
}

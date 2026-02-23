using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Ship;

public class TransferCargoRequest
{
    [Required]
    public Guid BoxedAssetId { get; set; }

    [Required]
    public Guid SourceShipId { get; set; }

    [Required]
    public Guid DestinationShipId { get; set; }

    [Required]
    public Guid DestinationCargoModuleId { get; set; }
}

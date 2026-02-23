using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Ship;

public class MoveCargoRequest
{
    [Required]
    public Guid BoxedAssetId { get; set; }

    [Required]
    public Guid DestinationCargoModuleId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Ship;

public class SplitCargoRequest
{
    [Required]
    public Guid BoxedAssetId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "SplitQuantity must be greater than zero.")]
    public decimal SplitQuantity { get; set; }
}

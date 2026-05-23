using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Mint;

[Serializable]
public class MintBurnRequestModel
{
    [Required]
    public decimal Amount { get; init; }
}

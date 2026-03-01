using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.Models.Mint;

[Serializable]
public class MintBurnRequestModel
{
    [Required]
    public decimal Amount { get; init; }
}

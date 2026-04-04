using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.User;

public class TokenAmountRequestModel
{
    [Range(0.1, (double)decimal.MaxValue)]
    public decimal Amount { get; set; }
}

namespace psecsapi.api.models.User;

public class InvestTokensResponseModel
{
    public decimal NewInvestedTotal { get; set; }
    public decimal NewAvailableTokens { get; set; }
    public int TrancheCount { get; set; }
}

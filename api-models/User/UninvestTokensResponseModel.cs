namespace psecsapi.api.models.User;

public class UninvestTokensResponseModel
{
    public decimal NewInvestedTotal { get; set; }
    public decimal NewAvailableTokens { get; set; }
    public decimal TokensUninvested { get; set; }
}

namespace psecsapi.api.models.User;

public class StakeTokensResponseModel
{
    public decimal StakedTokens { get; set; }
    public decimal AvailableTokens { get; set; }
    public int RateLimit { get; set; }
    public string AccessToken { get; set; } = string.Empty;
}

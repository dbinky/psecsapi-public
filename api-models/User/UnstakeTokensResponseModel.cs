namespace psecsapi.api.models.User;

public class UnstakeTokensResponseModel
{
    public decimal StakedTokens { get; set; }
    public decimal AvailableTokens { get; set; }
    public int RateLimit { get; set; }
    public DateTime CooldownEndsAt { get; set; }
    public string AccessToken { get; set; } = string.Empty;
}

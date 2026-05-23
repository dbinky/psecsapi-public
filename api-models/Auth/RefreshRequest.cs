namespace psecsapi.api.models.Auth;

public class RefreshRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

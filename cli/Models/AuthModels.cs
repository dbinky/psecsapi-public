namespace psecsapi.Console.Models
{
    public record AuthResponse(string AccessToken, string RefreshToken, string UserId, string DisplayName);
}

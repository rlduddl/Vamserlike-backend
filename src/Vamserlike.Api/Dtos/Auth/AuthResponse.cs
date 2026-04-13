namespace Vamserlike.Api.Dtos.Auth;

public class AuthResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;

    // TODO: 나중에 JWT 붙일 때 실제 토큰으로 교체
    public string AccessToken { get; set; } = string.Empty;
}
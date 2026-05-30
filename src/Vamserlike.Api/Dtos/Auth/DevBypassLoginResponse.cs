using Vamserlike.Api.Dtos.Game;

namespace Vamserlike.Api.Dtos.Auth;

public class DevBypassLoginResponse
{
    // Cognito 로그인 토큰
    public LoginResponse Auth { get; set; } = new();

    // 플레이어 정보
    public PlayerMeResponse Player { get; set; } = new();
}
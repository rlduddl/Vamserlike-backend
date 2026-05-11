namespace Vamserlike.Api.Dtos.Auth;

public class AuthMeResponse
{
    // Cognito sub
    public string UserId { get; set; } = string.Empty;

    // 로그인 이메일
    public string Email { get; set; } = string.Empty;

    // 토큰에서 읽은 사용자명
    public string UserName { get; set; } = string.Empty;
}
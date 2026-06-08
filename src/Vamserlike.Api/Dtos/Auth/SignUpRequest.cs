namespace Vamserlike.Api.Dtos.Auth;

public class SignUpRequest
{
    // 로그인용 이메일
    public string Email { get; set; } = string.Empty;

    // 로그인용 비밀번호
    public string Password { get; set; } = string.Empty;

    // 게임 내 닉네임
    // Cognito의 name 속성으로 저장됨
    public string Nickname { get; set; } = string.Empty;
}
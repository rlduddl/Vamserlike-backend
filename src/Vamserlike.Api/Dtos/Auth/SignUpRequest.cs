namespace Vamserlike.Api.Dtos.Auth;

public class SignUpRequest
{
    // 로그인용 이메일
    public string Email { get; set; } = string.Empty;

    // 로그인용 비밀번호
    public string Password { get; set; } = string.Empty;
}
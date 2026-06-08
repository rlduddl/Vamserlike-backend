namespace Vamserlike.Api.Dtos.Auth;

public class DevBypassLoginRequest
{
    // 테스트용 이메일
    public string Email { get; set; } = string.Empty;

    // 테스트용 비밀번호
    public string Password { get; set; } = "Password123!";

    // 테스트용 닉네임
    public string Nickname { get; set; } = string.Empty;
}
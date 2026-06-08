namespace Vamserlike.Api.Dtos.Auth;

public class DevLoginRequest
{
    // 테스트용 유저 ID
    public string UserId { get; set; } = string.Empty;

    // 테스트용 이메일
    public string Email { get; set; } = string.Empty;

    // 테스트용 닉네임
    public string Nickname { get; set; } = string.Empty;
}
namespace Vamserlike.Api.Dtos.Auth;

public class LoginResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool RequiresConfirmation { get; set; }
    public bool CanProceedToSignup { get; set; }

    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
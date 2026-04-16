namespace Vamserlike.Api.Dtos.Auth;

public class ConfirmSignUpRequest
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
}
namespace Vamserlike.Api.Dtos.Auth;

public class AuthActionResponse
{
    public bool RequiresConfirmation { get; set; }
    public string NextStep { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? UserSub { get; set; }
}
using System.Security.Claims;
using Vamserlike.Api.Dtos.Auth;

namespace Vamserlike.Api.Services;

public interface IAuthService
{
    Task<AuthActionResponse> SignUpAsync(SignUpRequest request);
    Task<AuthActionResponse> ConfirmSignUpAsync(ConfirmSignUpRequest request);
    Task<AuthActionResponse> ResendConfirmationAsync(ResendConfirmationRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);

    AuthMeResponse GetCurrentUser(ClaimsPrincipal user);
}
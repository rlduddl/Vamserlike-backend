using Vamserlike.Api.Dtos.Auth;

namespace Vamserlike.Api.Services;

public interface IAuthService
{
    AuthResponse Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
}
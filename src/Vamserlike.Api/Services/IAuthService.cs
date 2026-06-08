using System.Security.Claims;
using Vamserlike.Api.Dtos.Auth;

namespace Vamserlike.Api.Services;

public interface IAuthService
{
    // 회원가입
    Task<AuthActionResponse> SignUpAsync(SignUpRequest request);

    // 이메일 인증코드 확인
    Task<AuthActionResponse> ConfirmSignUpAsync(ConfirmSignUpRequest request);

    // 로그인
    Task<LoginResponse> LoginAsync(LoginRequest request);

    // JWT 토큰에서 현재 사용자 정보 추출
    AuthMeResponse GetCurrentUser(ClaimsPrincipal user);
}
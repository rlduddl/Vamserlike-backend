using System.Security.Claims;
using Amazon.CognitoIdentityProvider;
using CognitoModel = Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Dtos.Auth;

namespace Vamserlike.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAmazonCognitoIdentityProvider _cognito;
    private readonly CognitoOptions _cognitoOptions;

    public AuthService(
        IAmazonCognitoIdentityProvider cognito,
        IOptions<CognitoOptions> cognitoOptions)
    {
        _cognito = cognito;
        _cognitoOptions = cognitoOptions.Value;
    }

    // Cognito App Client ID 반환
    private string ClientId =>
        string.IsNullOrWhiteSpace(_cognitoOptions.ClientId)
            ? throw new InvalidOperationException("Cognito ClientId가 설정되지 않았습니다.")
            : _cognitoOptions.ClientId;

    // 회원가입
    public async Task<AuthActionResponse> SignUpAsync(SignUpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var attributes = new List<CognitoModel.AttributeType>
        {
            // 사용자 이메일
            new CognitoModel.AttributeType
            {
                Name = "email",
                Value = email
            },

            // 게임 닉네임
            // Cognito의 name 속성 사용
            new CognitoModel.AttributeType
            {
                Name = "name",
                Value = request.Nickname
            }
        };

        var response = await _cognito.SignUpAsync(
            new CognitoModel.SignUpRequest
            {
                // Cognito App Client ID
                ClientId = ClientId,

                // 로그인 ID
                Username = email,

                // 로그인 비밀번호
                Password = request.Password,

                // 사용자 속성
                UserAttributes = attributes
            });

        var isConfirmed = response.UserConfirmed ?? false;

        return new AuthActionResponse
        {
            RequiresConfirmation = !isConfirmed,

            NextStep = isConfirmed
                ? "LOGIN"
                : "CONFIRM_SIGN_UP",

            Message = isConfirmed
                ? "회원가입이 완료되었습니다. 바로 로그인 가능합니다."
                : "회원가입 완료. 이메일 인증코드를 입력하세요.",

            UserSub = response.UserSub
        };
    }

    // 이메일 인증코드 확인
    public async Task<AuthActionResponse> ConfirmSignUpAsync(ConfirmSignUpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        await _cognito.ConfirmSignUpAsync(
            new CognitoModel.ConfirmSignUpRequest
            {
                ClientId = ClientId,
                Username = email,
                ConfirmationCode = request.ConfirmationCode.Trim()
            });

        return new AuthActionResponse
        {
            RequiresConfirmation = false,
            NextStep = "LOGIN",
            Message = "이메일 인증이 완료되었습니다. 다시 로그인하세요."
        };
    }

 

    // 로그인
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var response = await _cognito.InitiateAuthAsync(
            new CognitoModel.InitiateAuthRequest
            {
                ClientId = ClientId,

                // 이메일 + 비밀번호 로그인
                AuthFlow = "USER_PASSWORD_AUTH",

                AuthParameters = new Dictionary<string, string>
                {
                    ["USERNAME"] = email,
                    ["PASSWORD"] = request.Password
                }
            });

        var authResult = response.AuthenticationResult
            ?? throw new InvalidOperationException("로그인 응답에 토큰이 없습니다.");

        return new LoginResponse
        {
            Status = "LOGIN_SUCCESS",
            Message = "로그인 성공",

            RequiresConfirmation = false,
            CanProceedToSignup = false,

            AccessToken = authResult.AccessToken ?? string.Empty,
            IdToken = authResult.IdToken ?? string.Empty,
            RefreshToken = authResult.RefreshToken ?? string.Empty,

            ExpiresIn = authResult.ExpiresIn ?? 0,
            TokenType = authResult.TokenType ?? "Bearer"
        };
    }

    // JWT 토큰에서 현재 사용자 정보 추출
    public AuthMeResponse GetCurrentUser(ClaimsPrincipal user)
    {
        // Cognito 사용자 고유 ID
        var userId =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            string.Empty;

        // 게임 닉네임
        // Cognito name 속성 우선 사용
        var userName =
            user.FindFirst("name")?.Value ??
            user.FindFirst("username")?.Value ??
            user.FindFirst("cognito:username")?.Value ??
            user.FindFirst(ClaimTypes.Name)?.Value ??
            string.Empty;

        // 사용자 이메일
        var email =
            user.FindFirst("email")?.Value ??
            user.FindFirst(ClaimTypes.Email)?.Value ??
            string.Empty;

        // access token에는 email 클레임이 비어 있을 수 있음
        // username이 이메일 형태면 보정
        if (string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(userName) &&
            userName.Contains("@"))
        {
            email = userName;
        }

        return new AuthMeResponse
        {
            UserId = userId,
            Email = email,
            UserName = userName
        };
    }
}
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

    private string ClientId =>
        string.IsNullOrWhiteSpace(_cognitoOptions.ClientId)
            ? throw new InvalidOperationException("Cognito ClientId가 설정되지 않았습니다.")
            : _cognitoOptions.ClientId;

    public async Task<AuthActionResponse> SignUpAsync(SignUpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var response = await _cognito.SignUpAsync(
            new CognitoModel.SignUpRequest
            {
                ClientId = ClientId,
                Username = email,
                Password = request.Password,
                UserAttributes = new List<CognitoModel.AttributeType>
                {
                    new CognitoModel.AttributeType { Name = "email", Value = email },
                    new CognitoModel.AttributeType { Name = "name", Value = request.Nickname.Trim() }
                }
            });

        var isConfirmed = response.UserConfirmed ?? false;

        return new AuthActionResponse
        {
            RequiresConfirmation = !isConfirmed,
            NextStep = isConfirmed ? "LOGIN" : "CONFIRM_SIGN_UP",
            Message = isConfirmed
                ? "회원가입이 완료되었습니다. 바로 로그인 가능합니다."
                : "회원가입 완료. 이메일 인증코드를 입력하세요.",
            UserSub = response.UserSub
        };
    }

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

    public async Task<AuthActionResponse> ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        await _cognito.ResendConfirmationCodeAsync(
            new CognitoModel.ResendConfirmationCodeRequest
            {
                ClientId = ClientId,
                Username = email
            });

        return new AuthActionResponse
        {
            RequiresConfirmation = true,
            NextStep = "CONFIRM_SIGN_UP",
            Message = "인증코드를 다시 전송했습니다."
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var response = await _cognito.InitiateAuthAsync(
            new CognitoModel.InitiateAuthRequest
            {
                ClientId = ClientId,
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

    public AuthMeResponse GetCurrentUser(ClaimsPrincipal user)
    {
        var userId =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            string.Empty;

        var email =
            user.FindFirst("email")?.Value ??
            user.FindFirst(ClaimTypes.Email)?.Value ??
            string.Empty;

        var userName =
            user.FindFirst("username")?.Value ??              // access token에서 중요
            user.FindFirst("cognito:username")?.Value ??     // id token에서 올 수 있음
            user.FindFirst(ClaimTypes.Name)?.Value ??
            user.FindFirst("name")?.Value ??
            string.Empty;

        return new AuthMeResponse
        {
            UserId = userId,
            Email = email,
            UserName = userName
        };
    }
}
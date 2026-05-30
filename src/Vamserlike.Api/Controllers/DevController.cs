using Amazon.CognitoIdentityProvider;
using CognitoModel = Amazon.CognitoIdentityProvider.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Services;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IAmazonCognitoIdentityProvider _cognito;
    private readonly CognitoOptions _cognitoOptions;
    private readonly IAuthService _authService;
    private readonly IPlayerService _playerService;
    private readonly IPlayerRepository _playerRepository;
    private readonly ILogger<DevController> _logger;

    public DevController(
        IWebHostEnvironment env,
        IAmazonCognitoIdentityProvider cognito,
        IOptions<CognitoOptions> cognitoOptions,
        IAuthService authService,
        IPlayerService playerService,
        IPlayerRepository playerRepository,
        ILogger<DevController> logger)
    {
        _env = env;
        _cognito = cognito;
        _cognitoOptions = cognitoOptions.Value;
        _authService = authService;
        _playerService = playerService;
        _playerRepository = playerRepository;
        _logger = logger;
    }

    // 개발용 인증코드 회피 로그인
    // Cognito 유저 생성 + 강제 인증 + 비밀번호 설정 + 로그인 + Player Init
    [HttpPost("bypass-login")]
    public async Task<ActionResult<ApiResponse<DevBypassLoginResponse>>> BypassLogin(
        [FromBody] DevBypassLoginRequest request)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound(ApiResponse<DevBypassLoginResponse>.Fail(
                "개발 환경에서만 사용 가능한 API입니다."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password;
        var nickname = request.Nickname.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<DevBypassLoginResponse>.Fail(
                "Email은 필수입니다."));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(ApiResponse<DevBypassLoginResponse>.Fail(
                "Password는 필수입니다."));
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            return BadRequest(ApiResponse<DevBypassLoginResponse>.Fail(
                "Nickname은 필수입니다."));
        }

        if (string.IsNullOrWhiteSpace(_cognitoOptions.UserPoolId))
        {
            return BadRequest(ApiResponse<DevBypassLoginResponse>.Fail(
                "Cognito UserPoolId가 설정되지 않았습니다."));
        }

        try
        {
            await EnsureCognitoUserAsync(email, password, nickname);

            var auth = await _authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            });

            var userSub = await GetUserSubAsync(email);

            var currentUser = new AuthMeResponse
            {
                UserId = userSub,
                Email = email,
                UserName = nickname
            };

            var player = await _playerService.InitAsync(currentUser);

            _logger.LogInformation(
                "DevBypassLoginSuccess Email={Email} UserSub={UserSub} Nickname={Nickname}",
                email,
                userSub,
                nickname);

            var response = new DevBypassLoginResponse
            {
                Auth = auth,
                Player = player
            };

            return Ok(ApiResponse<DevBypassLoginResponse>.Ok(
                response,
                "개발용 인증코드 회피 로그인 완료"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DevBypassLoginFailed Email={Email}",
                email);

            return BadRequest(ApiResponse<DevBypassLoginResponse>.Fail(
                ex.Message));
        }
    }

    // 개발용 테스트 데이터 전체 초기화
    // DynamoDB 플레이어 전체 삭제 + Cognito 유저 삭제
    [HttpDelete("reset-test-data")]
    public async Task<ActionResult<ApiResponse<DevResetResponse>>> ResetTestData(
        [FromBody] DevResetRequest request)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound(ApiResponse<DevResetResponse>.Fail(
                "개발 환경에서만 사용 가능한 API입니다."));
        }

        if (request.ConfirmText != "DELETE_TEST_DATA")
        {
            return BadRequest(ApiResponse<DevResetResponse>.Fail(
                "ConfirmText 값이 올바르지 않습니다. DELETE_TEST_DATA 를 입력하세요."));
        }

        try
        {
            var deletedDbPlayers = await _playerRepository.DeleteAllAsync();

            var deletedCognitoUsers = 0;

            if (request.DeleteAllCognitoUsers)
            {
                deletedCognitoUsers = await DeleteAllCognitoUsersAsync();
            }

            _logger.LogWarning(
                "DevResetTestData Completed DeletedDbPlayers={DeletedDbPlayers} DeletedCognitoUsers={DeletedCognitoUsers}",
                deletedDbPlayers,
                deletedCognitoUsers);

            var response = new DevResetResponse
            {
                DeletedDbPlayers = deletedDbPlayers,
                DeletedCognitoUsers = deletedCognitoUsers
            };

            return Ok(ApiResponse<DevResetResponse>.Ok(
                response,
                "개발용 테스트 데이터 초기화 완료"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DevResetTestDataFailed");

            return BadRequest(ApiResponse<DevResetResponse>.Fail(
                ex.Message));
        }
    }

    // Cognito 유저 없으면 생성, 있으면 속성/비밀번호 보정
    private async Task EnsureCognitoUserAsync(
        string email,
        string password,
        string nickname)
    {
        var exists = true;

        try
        {
            await _cognito.AdminGetUserAsync(
                new CognitoModel.AdminGetUserRequest
                {
                    UserPoolId = _cognitoOptions.UserPoolId,
                    Username = email
                });
        }
        catch (CognitoModel.UserNotFoundException)
        {
            exists = false;
        }

        if (!exists)
        {
            await _cognito.AdminCreateUserAsync(
                new CognitoModel.AdminCreateUserRequest
                {
                    UserPoolId = _cognitoOptions.UserPoolId,
                    Username = email,
                    MessageAction = "SUPPRESS",
                    UserAttributes = new List<CognitoModel.AttributeType>
                    {
                        new CognitoModel.AttributeType
                        {
                            Name = "email",
                            Value = email
                        },
                        new CognitoModel.AttributeType
                        {
                            Name = "email_verified",
                            Value = "true"
                        },
                        new CognitoModel.AttributeType
                        {
                            Name = "name",
                            Value = nickname
                        }
                    }
                });
        }
        else
        {
            await _cognito.AdminUpdateUserAttributesAsync(
                new CognitoModel.AdminUpdateUserAttributesRequest
                {
                    UserPoolId = _cognitoOptions.UserPoolId,
                    Username = email,
                    UserAttributes = new List<CognitoModel.AttributeType>
                    {
                        new CognitoModel.AttributeType
                        {
                            Name = "email_verified",
                            Value = "true"
                        },
                        new CognitoModel.AttributeType
                        {
                            Name = "name",
                            Value = nickname
                        }
                    }
                });
        }

        await _cognito.AdminSetUserPasswordAsync(
            new CognitoModel.AdminSetUserPasswordRequest
            {
                UserPoolId = _cognitoOptions.UserPoolId,
                Username = email,
                Password = password,
                Permanent = true
            });
    }

    // Cognito sub 조회
    private async Task<string> GetUserSubAsync(string email)
    {
        var user = await _cognito.AdminGetUserAsync(
            new CognitoModel.AdminGetUserRequest
            {
                UserPoolId = _cognitoOptions.UserPoolId,
                Username = email
            });

        return user.UserAttributes
            .FirstOrDefault(x => x.Name == "sub")
            ?.Value ?? string.Empty;
    }

    // Cognito 사용자 전체 삭제
    private async Task<int> DeleteAllCognitoUsersAsync()
    {
        var deletedCount = 0;
        string? paginationToken = null;

        do
        {
            var response = await _cognito.ListUsersAsync(
                new CognitoModel.ListUsersRequest
                {
                    UserPoolId = _cognitoOptions.UserPoolId,
                    PaginationToken = paginationToken
                });

            foreach (var user in response.Users)
            {
                if (string.IsNullOrWhiteSpace(user.Username))
                {
                    continue;
                }

                await _cognito.AdminDeleteUserAsync(
                    new CognitoModel.AdminDeleteUserRequest
                    {
                        UserPoolId = _cognitoOptions.UserPoolId,
                        Username = user.Username
                    });

                deletedCount++;
            }

            paginationToken = response.PaginationToken;

        } while (!string.IsNullOrWhiteSpace(paginationToken));

        return deletedCount;
    }
}
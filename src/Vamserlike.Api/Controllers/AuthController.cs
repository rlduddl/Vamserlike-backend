using Amazon.Runtime.Internal;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Services;

using CognitoModel = Amazon.CognitoIdentityProvider.Model;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<HealthController> _logger;

    public AuthController(IAuthService authService, ILogger<HealthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // 회원가입
    // email + password + nickname(name)
    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse<AuthActionResponse>>> SignUp(
        [FromBody] SignUpRequest request)
    {
        try
        {
            var result = await _authService.SignUpAsync(request);

            return Ok(ApiResponse<AuthActionResponse>.Ok(
                result,
                result.Message));
        }
        catch (CognitoModel.UsernameExistsException)
        {
            return Conflict(ApiResponse<AuthActionResponse>.Fail(
                "이미 가입된 이메일입니다."));
        }
        catch (CognitoModel.InvalidPasswordException ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(
                $"비밀번호 정책 오류: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(
                ex.Message));
        }
    }

    // 이메일 인증코드 확인
    [HttpPost("confirm-signup")]
    public async Task<ActionResult<ApiResponse<AuthActionResponse>>> ConfirmSignUp(
        [FromBody] ConfirmSignUpRequest request)
    {
        try
        {
            var result = await _authService.ConfirmSignUpAsync(request);

            //회원가입 로그
            _logger.LogInformation("{@LogData}", new
            {
                EventName = "UserRegist",
                Email = request.Email, // 유저 식별자
                Status = "ok",
                
                UtcNow = DateTime.UtcNow
            });

            return Ok(ApiResponse<AuthActionResponse>.Ok(
                result,
                result.Message));
        }
        catch (CognitoModel.CodeMismatchException)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(
                "인증코드가 올바르지 않습니다."));
        }
        catch (CognitoModel.ExpiredCodeException)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(
                "인증코드가 만료되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(
                ex.Message));
        }
    }

    // 로그인
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            //로그인 로그
            _logger.LogInformation("{@LogData}", new
            {
                EventName = "UserLogin",
                Email = request.Email, // 유저 식별자
                Status = result.Status,
                // 필요하다면 토큰 만료 시간 같은 것도 추가 가능
                ExpiresIn = result.ExpiresIn,
                UtcNow = DateTime.UtcNow
            });


            return Ok(ApiResponse<LoginResponse>.Ok(
                result,
                result.Message));
        }
        catch (CognitoModel.UserNotConfirmedException)
        {
            var response = new LoginResponse
            {
                // 이메일 인증 필요
                Status = "CONFIRM_REQUIRED",

                Message =
                    "이메일 인증이 완료되지 않았습니다. 인증 후 다시 로그인하세요.",

                RequiresConfirmation = true,
                CanProceedToSignup = false
            };

            return Conflict(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = response.Message,
                Data = response
            });
        }
        catch (CognitoModel.UserNotFoundException)
        {
            var response = new LoginResponse
            {
                // 회원가입 필요
                Status = "SIGNUP_SUGGESTED",

                Message =
                    "계정이 없습니다. 회원가입을 진행하세요.",

                RequiresConfirmation = false,
                CanProceedToSignup = true
            };

            return NotFound(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = response.Message,
                Data = response
            });
        }
        catch (CognitoModel.NotAuthorizedException)
        {
            var response = new LoginResponse
            {
                // 비밀번호 불일치
                Status = "INVALID_CREDENTIALS",

                Message =
                    "이메일 또는 비밀번호가 올바르지 않습니다.",

                RequiresConfirmation = false,
                CanProceedToSignup = false
            };

            return Unauthorized(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = response.Message,
                Data = response
            });
        }
        catch (Exception ex)
        {
            var response = new LoginResponse
            {
                // 기타 로그인 실패
                Status = "LOGIN_FAILED",

                Message = ex.Message,

                RequiresConfirmation = false,
                CanProceedToSignup = false
            };

            return BadRequest(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = response.Message,
                Data = response
            });
        }
    }

    // 현재 로그인 사용자 정보 조회
    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<AuthMeResponse>> Me()
    {
        var me = _authService.GetCurrentUser(User);
        return Ok(ApiResponse<AuthMeResponse>.Ok(
            me,
            "현재 로그인 사용자"));
    }
}
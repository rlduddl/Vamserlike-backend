using Microsoft.AspNetCore.Mvc;
using CognitoModel = Amazon.CognitoIdentityProvider.Model;
using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Services;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse<AuthActionResponse>>> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            var result = await _authService.SignUpAsync(request);
            return Ok(ApiResponse<AuthActionResponse>.Ok(result, result.Message));
        }
        catch (CognitoModel.UsernameExistsException)
        {
            return Conflict(ApiResponse<AuthActionResponse>.Fail("이미 가입된 이메일입니다."));
        }
        catch (CognitoModel.InvalidPasswordException ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail($"비밀번호 정책 오류: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("confirm-signup")]
    public async Task<ActionResult<ApiResponse<AuthActionResponse>>> ConfirmSignUp([FromBody] ConfirmSignUpRequest request)
    {
        try
        {
            var result = await _authService.ConfirmSignUpAsync(request);
            return Ok(ApiResponse<AuthActionResponse>.Ok(result, result.Message));
        }
        catch (CognitoModel.CodeMismatchException)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail("인증코드가 올바르지 않습니다."));
        }
        catch (CognitoModel.ExpiredCodeException)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail("인증코드가 만료되었습니다. 재전송 후 다시 시도하세요."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("resend-confirmation")]
    public async Task<ActionResult<ApiResponse<AuthActionResponse>>> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        try
        {
            var result = await _authService.ResendConfirmationAsync(request);
            return Ok(ApiResponse<AuthActionResponse>.Ok(result, result.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthActionResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponse>.Ok(result, "로그인 성공"));
        }
        catch (CognitoModel.UserNotConfirmedException)
        {
            return Conflict(ApiResponse<LoginResponse>.Fail("이메일 인증이 완료되지 않았습니다. 인증 후 다시 로그인하세요."));
        }
        catch (CognitoModel.NotAuthorizedException)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("이메일 또는 비밀번호가 올바르지 않습니다."));
        }
        catch (CognitoModel.UserNotFoundException)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("이메일 또는 비밀번호가 올바르지 않습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.Fail(ex.Message));
        }
    }
}
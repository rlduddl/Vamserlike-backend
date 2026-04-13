using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("register")]
    public ActionResult<ApiResponse<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = _authService.Register(request);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "회원가입 성공"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = _authService.Login(request);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "로그인 성공"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
    }
}
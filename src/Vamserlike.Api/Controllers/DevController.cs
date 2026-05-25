using Microsoft.AspNetCore.Mvc;
using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Dtos.Game;
using Vamserlike.Api.Services;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IPlayerService _playerService;

    public DevController(
        IWebHostEnvironment env,
        IPlayerService playerService)
    {
        _env = env;
        _playerService = playerService;
    }

    // 개발 테스트용 치트 로그인
    // 운영 환경에서는 사용 불가
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<PlayerMeResponse>>> DevLogin(
        [FromBody] DevLoginRequest request)
    {
        // Production 환경 차단
        if (!_env.IsDevelopment())
        {
            return NotFound(ApiResponse<PlayerMeResponse>.Fail(
                "개발 환경에서만 사용 가능한 API입니다."));
        }

        // UserId 자동 생성
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            request.UserId = $"dev-{request.Email}";
        }

        // 테스트용 현재 사용자 생성
        var currentUser = new AuthMeResponse
        {
            UserId = request.UserId,
            Email = request.Email,
            UserName = request.Nickname
        };

        // 플레이어 초기화
        var result = await _playerService.InitAsync(currentUser);

        return Ok(ApiResponse<PlayerMeResponse>.Ok(
            result,
            "개발용 치트 로그인 완료"));
    }
}
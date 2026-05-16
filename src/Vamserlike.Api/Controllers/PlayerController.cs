using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vamserlike.Api.Dtos.Common;
using Vamserlike.Api.Dtos.Game;
using Vamserlike.Api.Services;

namespace Vamserlike.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/players")]
public class PlayerController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPlayerService _playerService;

    public PlayerController(
        IAuthService authService,
        IPlayerService playerService)
    {
        _authService = authService;
        _playerService = playerService;
    }

    // 로그인 후 최초 플레이어 데이터 생성
    [HttpPost("me/init")]
    public async Task<ActionResult<ApiResponse<PlayerMeResponse>>> InitMe()
    {
        var currentUser = _authService.GetCurrentUser(User);
        var result = await _playerService.InitAsync(currentUser);

        return Ok(ApiResponse<PlayerMeResponse>.Ok(result, "player initialized"));
    }

    // 내 플레이어 정보 조회
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<PlayerMeResponse>>> GetMe()
    {
        var currentUser = _authService.GetCurrentUser(User);
        var result = await _playerService.GetMeAsync(currentUser);

        return Ok(ApiResponse<PlayerMeResponse>.Ok(result, "player state"));
    }

    // 게임 결과 저장
    [HttpPut("me/progress")]
    public async Task<ActionResult<ApiResponse<PlayerMeResponse>>> UpdateProgress(
        [FromBody] UpdateProgressRequest request)
    {
        try
        {
            var currentUser = _authService.GetCurrentUser(User);
            var result = await _playerService.UpdateProgressAsync(currentUser, request);

            return Ok(ApiResponse<PlayerMeResponse>.Ok(result, "progress updated"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PlayerMeResponse>.Fail(ex.Message));
        }
    }

    // 닉네임 저장 또는 수정
    [HttpPut("me/nickname")]
    public async Task<ActionResult<ApiResponse<PlayerMeResponse>>> SetNickname(
        [FromBody] SetNicknameRequest request)
    {
        try
        {
            var currentUser = _authService.GetCurrentUser(User);
            var result = await _playerService.SetNicknameAsync(currentUser, request);

            return Ok(ApiResponse<PlayerMeResponse>.Ok(result, "nickname updated"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PlayerMeResponse>.Fail(ex.Message));
        }
    }

    // 랭킹 조회
    [AllowAnonymous]
    [HttpGet("ranking")]
    public async Task<ActionResult<ApiResponse<List<RankingItemResponse>>>> GetRanking([FromQuery] int take = 20)
    {
        var result = await _playerService.GetRankingAsync(take);

        return Ok(ApiResponse<List<RankingItemResponse>>.Ok(result, "ranking loaded"));
    }
}
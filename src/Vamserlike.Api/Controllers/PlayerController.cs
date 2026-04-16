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

    [HttpPost("me/init")]
    public async Task<ActionResult<ApiResponse<PlayerStateResponse>>> InitMe()
    {
        var currentUser = _authService.GetCurrentUser(User);
        var result = await _playerService.InitializeMyPlayerAsync(currentUser);

        return Ok(ApiResponse<PlayerStateResponse>.Ok(result, "player initialized"));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<PlayerStateResponse>>> GetMe()
    {
        var currentUser = _authService.GetCurrentUser(User);
        var result = await _playerService.GetMyStateAsync(currentUser);

        return Ok(ApiResponse<PlayerStateResponse>.Ok(result, "player state"));
    }

    [HttpPut("me/progress")]
    public async Task<ActionResult<ApiResponse<PlayerStateResponse>>> UpdateProgress(
        [FromBody] UpdateProgressRequest request)
    {
        var currentUser = _authService.GetCurrentUser(User);
        var result = await _playerService.UpdateMyProgressAsync(currentUser, request);

        return Ok(ApiResponse<PlayerStateResponse>.Ok(result, "progress updated"));
    }
}
using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Game;
using Vamserlike.Api.Models;
using Vamserlike.Api.Repositories;

namespace Vamserlike.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerStateResponse> InitializeMyPlayerAsync(AuthMeResponse currentUser)
    {
        await _playerRepository.InitializePlayerAsync(
            currentUser.UserId,
            currentUser.UserName,
            currentUser.Email);

        var profile = await _playerRepository.GetProfileAsync(currentUser.UserId);
        var progress = await _playerRepository.GetProgressAsync(currentUser.UserId);

        return new PlayerStateResponse
        {
            Profile = profile,
            Progress = progress
        };
    }

    public async Task<PlayerStateResponse> GetMyStateAsync(AuthMeResponse currentUser)
    {
        await _playerRepository.InitializePlayerAsync(
            currentUser.UserId,
            currentUser.UserName,
            currentUser.Email);

        var profile = await _playerRepository.GetProfileAsync(currentUser.UserId);
        var progress = await _playerRepository.GetProgressAsync(currentUser.UserId);

        return new PlayerStateResponse
        {
            Profile = profile,
            Progress = progress
        };
    }

    public async Task<PlayerStateResponse> UpdateMyProgressAsync(AuthMeResponse currentUser, UpdateProgressRequest request)
    {
        await _playerRepository.InitializePlayerAsync(
            currentUser.UserId,
            currentUser.UserName,
            currentUser.Email);

        var current = await _playerRepository.GetProgressAsync(currentUser.UserId)
                     ?? new PlayerProgress
                     {
                         UserId = currentUser.UserId
                     };

        current.Gold = Math.Max(0, request.Gold);
        current.TotalKills = Math.Max(0, request.TotalKills);
        current.PlayedSeconds = Math.Max(0, request.PlayedSeconds);

        await _playerRepository.SaveProgressAsync(current);

        var profile = await _playerRepository.GetProfileAsync(currentUser.UserId);

        return new PlayerStateResponse
        {
            Profile = profile,
            Progress = current
        };
    }
}
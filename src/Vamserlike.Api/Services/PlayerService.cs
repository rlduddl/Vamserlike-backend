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

    // 로그인 후 최초 플레이어 데이터 생성
    public async Task<PlayerMeResponse> InitAsync(AuthMeResponse currentUser)
    {
        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = new PlayerProfile
            {
                UserId = currentUser.UserId,
                Email = currentUser.Email,
                Nickname = "guest",
                SelectedCharacterId = "rice_farmer",
                LastPlayedCharacterId = "rice_farmer",
                BestScore = 0,
                HighestLevel = 0,
                TotalPlayCount = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _playerRepository.PutAsync(profile);
        }

        return ToResponse(profile);
    }

    // 내 플레이어 정보 조회
    public async Task<PlayerMeResponse> GetMeAsync(AuthMeResponse currentUser)
    {
        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = new PlayerProfile
            {
                UserId = currentUser.UserId,
                Email = currentUser.Email,
                Nickname = string.IsNullOrWhiteSpace(currentUser.UserName) ? "guest" : currentUser.UserName,
                SelectedCharacterId = "rice_farmer",
                LastPlayedCharacterId = "rice_farmer",
                BestScore = 0,
                HighestLevel = 0,
                TotalPlayCount = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _playerRepository.PutAsync(profile);
        }

        return ToResponse(profile);
    }

    // 게임 종료 후 점수/레벨/플레이 캐릭터 반영
    public async Task<PlayerMeResponse> UpdateProgressAsync(AuthMeResponse currentUser, UpdateProgressRequest request)
    {
        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = new PlayerProfile
            {
                UserId = currentUser.UserId,
                Email = currentUser.Email,
                Nickname = string.IsNullOrWhiteSpace(currentUser.UserName) ? "guest" : currentUser.UserName,
                SelectedCharacterId = "rice_farmer",
                LastPlayedCharacterId = "rice_farmer",
                BestScore = 0,
                HighestLevel = 0,
                TotalPlayCount = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        profile.LastPlayedCharacterId = string.IsNullOrWhiteSpace(request.PlayedCharacterId)
            ? profile.LastPlayedCharacterId
            : request.PlayedCharacterId;

        profile.TotalPlayCount += 1;

        if (request.Score > profile.BestScore)
        {
            profile.BestScore = request.Score;
            profile.SelectedCharacterId = profile.LastPlayedCharacterId;
        }

        if (request.Level > profile.HighestLevel)
        {
            profile.HighestLevel = request.Level;
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _playerRepository.PutAsync(profile);

        return ToResponse(profile);
    }

    // 닉네임 저장 또는 수정
    public async Task<PlayerMeResponse> SetNicknameAsync(AuthMeResponse currentUser, SetNicknameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            throw new ArgumentException("닉네임은 비워둘 수 없습니다.");
        }

        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = new PlayerProfile
            {
                UserId = currentUser.UserId,
                Email = currentUser.Email,
                SelectedCharacterId = "rice_farmer",
                LastPlayedCharacterId = "rice_farmer",
                BestScore = 0,
                HighestLevel = 0,
                TotalPlayCount = 0
            };
        }

        profile.Nickname = request.Nickname.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _playerRepository.PutAsync(profile);

        return ToResponse(profile);
    }

    // PlayerProfile -> 응답 DTO 변환
    private static PlayerMeResponse ToResponse(PlayerProfile profile)
    {
        return new PlayerMeResponse
        {
            UserId = profile.UserId,
            Email = profile.Email,
            Nickname = profile.Nickname,
            SelectedCharacterId = profile.SelectedCharacterId,
            LastPlayedCharacterId = profile.LastPlayedCharacterId,
            BestScore = profile.BestScore,
            HighestLevel = profile.HighestLevel,
            TotalPlayCount = profile.TotalPlayCount
        };
    }
}
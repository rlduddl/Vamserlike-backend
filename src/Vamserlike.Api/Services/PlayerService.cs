using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Game;
using Vamserlike.Api.Models;
using Vamserlike.Api.Repositories;

namespace Vamserlike.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    // 캐릭터 해금 기준
    private const int PotatoFarmerUnlockKillCount = 100;
    private const int BeanFarmerUnlockKillCount = 300;

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
                // Cognito sub
                UserId = currentUser.UserId,

                // 로그인 이메일
                Email = currentUser.Email,

                // Cognito name 속성
                // Unity 회원가입 닉네임 사용
                Nickname = string.IsNullOrWhiteSpace(currentUser.UserName)
                    ? "guest"
                    : currentUser.UserName,

                // 기본 선택 캐릭터
                SelectedCharacterId = "rice_farmer",

                // 마지막 플레이 캐릭터
                LastPlayedCharacterId = "rice_farmer",

                // 최고 점수
                BestScore = 0,

                // 최고 레벨
                HighestLevel = 0,

                // 총 플레이 횟수
                TotalPlayCount = 0,

                // 누적 적 처치 수
                TotalKillCount = 0,

                // 기본 지급 캐릭터
                UnlockedCharacterIds = new List<string>
                {
                    "rice_farmer",
                    "barley_farmer"
                },

                // 마지막 수정 시각
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _playerRepository.PutAsync(profile);
        }
        else
        {
            var changed = false;

            // 이메일 보정
            if (string.IsNullOrWhiteSpace(profile.Email) &&
                !string.IsNullOrWhiteSpace(currentUser.Email))
            {
                profile.Email = currentUser.Email;
                changed = true;
            }

            // 닉네임 보정
            if (string.IsNullOrWhiteSpace(profile.Nickname) &&
                !string.IsNullOrWhiteSpace(currentUser.UserName))
            {
                profile.Nickname = currentUser.UserName;
                changed = true;
            }

            // 기본 캐릭터 보정
            if (profile.UnlockedCharacterIds == null ||
                profile.UnlockedCharacterIds.Count == 0)
            {
                profile.UnlockedCharacterIds = new List<string>
                {
                    "rice_farmer",
                    "barley_farmer"
                };

                changed = true;
            }

            var unlockChanged = ApplyCharacterUnlocks(profile);

            changed = changed || unlockChanged;

            if (changed)
            {
                profile.UpdatedAtUtc = DateTime.UtcNow;

                await _playerRepository.PutAsync(profile);
            }
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
                // Cognito sub
                UserId = currentUser.UserId,

                // 로그인 이메일
                Email = currentUser.Email,

                // Cognito name 속성
                // Unity 회원가입 닉네임 사용
                Nickname = string.IsNullOrWhiteSpace(currentUser.UserName)
                    ? "guest"
                    : currentUser.UserName,

                // 기본 선택 캐릭터
                SelectedCharacterId = "rice_farmer",

                // 마지막 플레이 캐릭터
                LastPlayedCharacterId = "rice_farmer",

                // 최고 점수
                BestScore = 0,

                // 최고 레벨
                HighestLevel = 0,

                // 총 플레이 횟수
                TotalPlayCount = 0,

                // 누적 적 처치 수
                TotalKillCount = 0,

                // 기본 지급 캐릭터
                UnlockedCharacterIds = new List<string>
                {
                    "rice_farmer",
                    "barley_farmer"
                },

                // 마지막 수정 시각
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _playerRepository.PutAsync(profile);
        }
        else
        {
            var changed = false;

            // 이메일 보정
            if (string.IsNullOrWhiteSpace(profile.Email) &&
                !string.IsNullOrWhiteSpace(currentUser.Email))
            {
                profile.Email = currentUser.Email;
                changed = true;
            }

            // 닉네임 보정
            if (string.IsNullOrWhiteSpace(profile.Nickname) &&
                !string.IsNullOrWhiteSpace(currentUser.UserName))
            {
                profile.Nickname = currentUser.UserName;
                changed = true;
            }

            // 기본 캐릭터 보정
            if (profile.UnlockedCharacterIds == null ||
                profile.UnlockedCharacterIds.Count == 0)
            {
                profile.UnlockedCharacterIds = new List<string>
                {
                    "rice_farmer",
                    "barley_farmer"
                };

                changed = true;
            }

            var unlockChanged = ApplyCharacterUnlocks(profile);

            changed = changed || unlockChanged;

            if (changed)
            {
                profile.UpdatedAtUtc = DateTime.UtcNow;

                await _playerRepository.PutAsync(profile);
            }
        }

        return ToResponse(profile);
    }

    // 게임 종료 후 결과 저장
    // score = 이번 판 적 처치 수
    public async Task<PlayerMeResponse> UpdateProgressAsync(
        AuthMeResponse currentUser,
        UpdateProgressRequest request)
    {
        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = new PlayerProfile
            {
                // Cognito sub
                UserId = currentUser.UserId,

                // 로그인 이메일
                Email = currentUser.Email,

                // Cognito name 속성
                // Unity 회원가입 닉네임 사용
                Nickname = string.IsNullOrWhiteSpace(currentUser.UserName)
                    ? "guest"
                    : currentUser.UserName,

                // 기본 선택 캐릭터
                SelectedCharacterId = "rice_farmer",

                // 마지막 플레이 캐릭터
                LastPlayedCharacterId = "rice_farmer",

                // 최고 점수
                BestScore = 0,

                // 최고 레벨
                HighestLevel = 0,

                // 총 플레이 횟수
                TotalPlayCount = 0,

                // 누적 적 처치 수
                TotalKillCount = 0,

                // 기본 지급 캐릭터
                UnlockedCharacterIds = new List<string>
                {
                    "rice_farmer",
                    "barley_farmer"
                },

                // 마지막 수정 시각
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        // 마지막 플레이 캐릭터 저장
        if (!string.IsNullOrWhiteSpace(request.PlayedCharacterId))
        {
            profile.LastPlayedCharacterId = request.PlayedCharacterId;
        }

        // 총 플레이 횟수 증가
        profile.TotalPlayCount += 1;

        // 점수 = 적 처치 수 누적
        profile.TotalKillCount += Math.Max(0, request.Score);

        // 최고 점수 갱신
        if (request.Score > profile.BestScore)
        {
            profile.BestScore = request.Score;

            profile.SelectedCharacterId =
                profile.LastPlayedCharacterId;
        }

        // 최고 레벨 갱신
        if (request.Level > profile.HighestLevel)
        {
            profile.HighestLevel = request.Level;
        }

        // 캐릭터 해금 처리
        ApplyCharacterUnlocks(profile);

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _playerRepository.PutAsync(profile);

        return ToResponse(profile);
    }

    // 랭킹 조회
    public async Task<List<RankingItemResponse>> GetRankingAsync(int take)
    {
        var players = await _playerRepository.GetAllAsync();

        var ranking = players
            .OrderByDescending(x => x.BestScore)
            .ThenByDescending(x => x.HighestLevel)
            .ThenBy(x => x.Nickname)
            .Take(take)
            .Select((player, index) => new RankingItemResponse
            {
                Rank = index + 1,

                Nickname = player.Nickname,

                BestScore = player.BestScore,

                HighestLevel = player.HighestLevel,

                TotalPlayCount = player.TotalPlayCount
            })
            .ToList();

        return ranking;
    }

    // 누적 적 처치 수에 따른 캐릭터 해금
    private static bool ApplyCharacterUnlocks(PlayerProfile profile)
    {
        var changed = false;

        // 기본 캐릭터 보정
        if (!profile.UnlockedCharacterIds.Contains("rice_farmer"))
        {
            profile.UnlockedCharacterIds.Add("rice_farmer");
            changed = true;
        }

        if (!profile.UnlockedCharacterIds.Contains("barley_farmer"))
        {
            profile.UnlockedCharacterIds.Add("barley_farmer");
            changed = true;
        }

        // 감자농부 해금
        if (profile.TotalKillCount >= PotatoFarmerUnlockKillCount &&
            !profile.UnlockedCharacterIds.Contains("potato_farmer"))
        {
            profile.UnlockedCharacterIds.Add("potato_farmer");
            changed = true;
        }

        // 콩농부 해금
        if (profile.TotalKillCount >= BeanFarmerUnlockKillCount &&
            !profile.UnlockedCharacterIds.Contains("bean_farmer"))
        {
            profile.UnlockedCharacterIds.Add("bean_farmer");
            changed = true;
        }

        return changed;
    }

    // PlayerProfile -> PlayerMeResponse 변환
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

            TotalPlayCount = profile.TotalPlayCount,

            TotalKillCount = profile.TotalKillCount,

            UnlockedCharacterIds = profile.UnlockedCharacterIds
        };
    }
}
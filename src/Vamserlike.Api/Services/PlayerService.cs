using Microsoft.Extensions.Options;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Game;
using Vamserlike.Api.Models;
using Vamserlike.Api.Repositories;

namespace Vamserlike.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly GameOptions _gameOptions;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        IPlayerRepository playerRepository,
        IOptions<GameOptions> gameOptions,
        ILogger<PlayerService> logger)
    {
        _playerRepository = playerRepository;
        _gameOptions = gameOptions.Value;
        _logger = logger;
    }

    // 로그인 후 최초 플레이어 데이터 생성
    public async Task<PlayerMeResponse> InitAsync(AuthMeResponse currentUser)
    {
        _logger.LogInformation(
            "PlayerInitRequested UserId={UserId} Email={Email} Nickname={Nickname}",
            currentUser.UserId,
            currentUser.Email,
            currentUser.UserName);

        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = CreateDefaultProfile(currentUser);

            await _playerRepository.PutAsync(profile);

            _logger.LogInformation(
                "PlayerProfileCreated UserId={UserId} Nickname={Nickname}",
                profile.UserId,
                profile.Nickname);
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

            if (profile.UnlockedCharacterIds == null)
            {
                profile.UnlockedCharacterIds = new List<string>();
                changed = true;
            }

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
        _logger.LogInformation(
            "PlayerMeRequested UserId={UserId} Email={Email}",
            currentUser.UserId,
            currentUser.Email);

        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = CreateDefaultProfile(currentUser);
            await _playerRepository.PutAsync(profile);
        }

        return ToResponse(profile);
    }

    // 게임 결과 저장
    public async Task<PlayerMeResponse> UpdateProgressAsync(
        AuthMeResponse currentUser,
        UpdateProgressRequest request)
    {
        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = CreateDefaultProfile(currentUser);
        }

        // 게임 결과 로그
        _logger.LogInformation(
            "GameResultReceived UserId={UserId} Nickname={Nickname} Character={Character} Score={Score} Level={Level} IsClear={IsClear}",
            profile.UserId,
            profile.Nickname,
            request.PlayedCharacterId,
            request.Score,
            request.Level,
            request.IsClear);

        // 마지막 플레이 캐릭터 저장
        if (!string.IsNullOrWhiteSpace(request.PlayedCharacterId))
        {
            profile.LastPlayedCharacterId = request.PlayedCharacterId;
        }

        // 총 플레이 횟수 증가
        profile.TotalPlayCount += 1;

        // 점수 = 적 처치 수 = 획득 골드
        var earnedGold = Math.Max(0, request.Score);

        profile.TotalKillCount += earnedGold;
        profile.Gold += earnedGold;

        // 최고 점수 갱신
        if (request.Score > profile.BestScore)
        {
            profile.BestScore = request.Score;
            profile.SelectedCharacterId = profile.LastPlayedCharacterId;
        }

        // 최고 레벨 갱신
        if (request.Level > profile.HighestLevel)
        {
            profile.HighestLevel = request.Level;
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _playerRepository.PutAsync(profile);

        _logger.LogInformation(
            "GameResultSaved UserId={UserId} Gold={Gold} TotalKillCount={TotalKillCount} BestScore={BestScore} HighestLevel={HighestLevel} TotalPlayCount={TotalPlayCount}",
            profile.UserId,
            profile.Gold,
            profile.TotalKillCount,
            profile.BestScore,
            profile.HighestLevel,
            profile.TotalPlayCount);

        return ToResponse(profile);
    }

    // 캐릭터 해금
    public async Task<PlayerMeResponse> UnlockCharacterAsync(
        AuthMeResponse currentUser,
        UnlockCharacterRequest request)
    {
        var characterId = request.CharacterId.Trim();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            throw new ArgumentException("CharacterId는 필수입니다.");
        }

        var profile = await _playerRepository.GetByUserIdAsync(currentUser.UserId);

        if (profile == null)
        {
            profile = CreateDefaultProfile(currentUser);
        }

        profile.UnlockedCharacterIds ??= new List<string>();

        _logger.LogInformation(
            "CharacterUnlockRequested UserId={UserId} Nickname={Nickname} CharacterId={CharacterId} CurrentGold={Gold}",
            profile.UserId,
            profile.Nickname,
            characterId,
            profile.Gold);

        if (profile.UnlockedCharacterIds.Contains(characterId))
        {
            _logger.LogInformation(
                "CharacterAlreadyUnlocked UserId={UserId} CharacterId={CharacterId}",
                profile.UserId,
                characterId);

            return ToResponse(profile);
        }

        var cost = GetCharacterUnlockCost(characterId);

        if (profile.Gold < cost)
        {
            throw new InvalidOperationException(
                $"골드가 부족합니다. 필요 골드: {cost}, 보유 골드: {profile.Gold}");
        }

        profile.Gold -= cost;
        profile.UnlockedCharacterIds.Add(characterId);

        if (string.IsNullOrWhiteSpace(profile.SelectedCharacterId))
        {
            profile.SelectedCharacterId = characterId;
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _playerRepository.PutAsync(profile);

        _logger.LogInformation(
            "CharacterUnlocked UserId={UserId} CharacterId={CharacterId} Cost={Cost} RemainingGold={Gold}",
            profile.UserId,
            characterId,
            cost,
            profile.Gold);

        return ToResponse(profile);
    }

    // 랭킹 조회
    public async Task<RankingResponse> GetRankingAsync(
        AuthMeResponse? currentUser,
        int take)
    {
        _logger.LogInformation(
            "RankingRequested UserId={UserId} Take={Take}",
            currentUser?.UserId ?? "anonymous",
            take);

        var players = await _playerRepository.GetAllAsync();

        var sorted = players
            .OrderByDescending(x => x.BestScore)
            .ThenByDescending(x => x.HighestLevel)
            .ThenByDescending(x => x.TotalKillCount)
            .ThenBy(x => x.Nickname)
            .ToList();

        var myRank = 0;

        if (currentUser != null &&
            !string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            var myIndex = sorted.FindIndex(x => x.UserId == currentUser.UserId);

            if (myIndex >= 0)
            {
                myRank = myIndex + 1;
            }
        }

        var items = sorted
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

        return new RankingResponse
        {
            MyRank = myRank,
            Items = items
        };
    }

    // 기본 플레이어 생성
    private static PlayerProfile CreateDefaultProfile(AuthMeResponse currentUser)
    {
        return new PlayerProfile
        {
            UserId = currentUser.UserId,
            Email = currentUser.Email,

            // Cognito name을 게임 닉네임으로 사용
            Nickname = string.IsNullOrWhiteSpace(currentUser.UserName)
                ? "guest"
                : currentUser.UserName,

            // 캐릭터는 클라가 open API 호출 전까지 빈값
            SelectedCharacterId = string.Empty,
            LastPlayedCharacterId = string.Empty,

            Gold = 0,
            BestScore = 0,
            HighestLevel = 0,
            TotalPlayCount = 0,
            TotalKillCount = 0,

            // 기본값은 빈 리스트
            UnlockedCharacterIds = new List<string>(),

            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    // appsettings에서 캐릭터 해금 비용 조회
    private int GetCharacterUnlockCost(string characterId)
    {
        if (_gameOptions.CharacterUnlockCosts.TryGetValue(characterId, out var cost))
        {
            return Math.Max(0, cost);
        }

        throw new InvalidOperationException(
            $"알 수 없는 캐릭터 ID입니다: {characterId}");
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
            Gold = profile.Gold,
            BestScore = profile.BestScore,
            HighestLevel = profile.HighestLevel,
            TotalPlayCount = profile.TotalPlayCount,
            TotalKillCount = profile.TotalKillCount,
            UnlockedCharacterIds = profile.UnlockedCharacterIds ?? new List<string>()
        };
    }
}
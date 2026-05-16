namespace Vamserlike.Api.Dtos.Game;

public class PlayerMeResponse
{
    // Cognito sub
    public string UserId { get; set; } = string.Empty;

    // 로그인 이메일
    public string Email { get; set; } = string.Empty;

    // 게임 닉네임
    public string Nickname { get; set; } = string.Empty;

    // 현재 선택 캐릭터
    public string SelectedCharacterId { get; set; } = string.Empty;

    // 마지막 플레이 캐릭터
    public string LastPlayedCharacterId { get; set; } = string.Empty;

    // 최고 점수
    public int BestScore { get; set; }

    // 최고 레벨
    public int HighestLevel { get; set; }

    // 총 플레이 횟수
    public int TotalPlayCount { get; set; }

    // 누적 적 처치 수
    public int TotalKillCount { get; set; }

    // 해금된 캐릭터 목록
    public List<string> UnlockedCharacterIds { get; set; } = new();
}
namespace Vamserlike.Api.Models;

public class PlayerProfile
{
    // Cognito sub
    public string UserId { get; set; } = string.Empty;

    // 로그인 이메일
    public string Email { get; set; } = string.Empty;

    // 게임 닉네임
    public string Nickname { get; set; } = "guest";

    // 현재 선택 캐릭터
    public string SelectedCharacterId { get; set; } = "rice_farmer";

    // 마지막 플레이 캐릭터
    public string LastPlayedCharacterId { get; set; } = "rice_farmer";

    // 최고 점수
    public int BestScore { get; set; } = 0;

    // 최고 레벨
    public int HighestLevel { get; set; } = 0;

    // 총 플레이 횟수
    public int TotalPlayCount { get; set; } = 0;

    // 누적 적 처치 수
    public int TotalKillCount { get; set; } = 0;

    // 해금된 캐릭터 목록
    public List<string> UnlockedCharacterIds { get; set; } = new()
    {
        "rice_farmer",
        "barley_farmer"
    };

    // 마지막 수정 시각
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
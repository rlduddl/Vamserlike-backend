namespace Vamserlike.Api.Models;

public class PlayerProfile
{
    // Cognito sub를 저장하는 고유 유저 ID
    public string UserId { get; set; } = string.Empty;

    // 로그인용 이메일
    public string Email { get; set; } = string.Empty;

    // 게임 내 표시용 닉네임
    public string Nickname { get; set; } = "guest";

    // 현재 선택한 캐릭터 ID
    public string SelectedCharacterId { get; set; } = "rice_farmer";

    // 마지막으로 플레이한 캐릭터 ID
    public string LastPlayedCharacterId { get; set; } = "rice_farmer";

    // 최고 점수
    public int BestScore { get; set; } = 0;

    // 최고 레벨
    public int HighestLevel { get; set; } = 0;

    // 총 플레이 횟수
    public int TotalPlayCount { get; set; } = 0;

    // 마지막 수정 시각
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
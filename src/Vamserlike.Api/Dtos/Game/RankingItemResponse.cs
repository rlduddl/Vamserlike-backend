namespace Vamserlike.Api.Dtos.Game;

public class RankingItemResponse
{
    // 랭킹 순위
    public int Rank { get; set; }

    // 플레이어 닉네임
    public string Nickname { get; set; } = string.Empty;

    // 최고 점수
    public int BestScore { get; set; }

    // 최고 레벨
    public int HighestLevel { get; set; }

    // 총 플레이 횟수
    public int TotalPlayCount { get; set; }
}
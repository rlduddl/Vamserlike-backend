namespace Vamserlike.Api.Dtos.Game;

public class RankingResponse
{
    // 내 순위
    public int MyRank { get; set; }

    // 랭킹 리스트
    public List<RankingItemResponse> Items { get; set; } = new();
}
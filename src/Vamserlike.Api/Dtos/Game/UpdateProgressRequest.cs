namespace Vamserlike.Api.Dtos.Game;

public class UpdateProgressRequest
{
    // 이번 판 점수
    public int Score { get; set; }

    // 이번 판 도달 레벨
    public int Level { get; set; }

    // 이번 판에 플레이한 캐릭터 ID
    public string PlayedCharacterId { get; set; } = string.Empty;
}
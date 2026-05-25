namespace Vamserlike.Api.Configurations;

public class GameOptions
{
    // 캐릭터별 해금 비용
    public Dictionary<string, int> CharacterUnlockCosts { get; set; } = new();
}
namespace Vamserlike.Api.Dtos.Game;

public class UnlockCharacterRequest
{
    // 해금할 캐릭터 ID
    public string CharacterId { get; set; } = string.Empty;
}
using Vamserlike.Api.Models;

namespace Vamserlike.Api.Dtos.Game;

public class PlayerStateResponse
{
    public PlayerProfile? Profile { get; set; }
    public PlayerProgress? Progress { get; set; }
}
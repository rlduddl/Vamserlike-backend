namespace Vamserlike.Api.Models;

public class PlayerProgress
{
    public string UserId { get; set; } = "";
    public int Gold { get; set; } = 0;
    public int TotalKills { get; set; } = 0;
    public int PlayedSeconds { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
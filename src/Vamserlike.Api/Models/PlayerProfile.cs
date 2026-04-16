namespace Vamserlike.Api.Models;

public class PlayerProfile
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Nickname { get; set; } = "guest";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    // future
    public int BestScore { get; set; } = 0;
    public int HighestLevel { get; set; } = 0;
}
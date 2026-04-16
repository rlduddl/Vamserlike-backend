namespace Vamserlike.Api.Dtos.Game;

public class UpdateProgressRequest
{
    public int Gold { get; set; }
    public int TotalKills { get; set; }
    public int PlayedSeconds { get; set; }
}
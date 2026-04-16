using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public interface IPlayerRepository
{
    Task InitializePlayerAsync(string userId, string userName, string email);
    Task<PlayerProfile?> GetProfileAsync(string userId);
    Task<PlayerProgress?> GetProgressAsync(string userId);
    Task SaveProgressAsync(PlayerProgress progress);
}
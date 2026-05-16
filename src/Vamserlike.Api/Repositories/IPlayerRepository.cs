using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public interface IPlayerRepository
{
    // 유저 ID로 플레이어 1명 조회
    Task<PlayerProfile?> GetByUserIdAsync(string userId);

    // 플레이어 전체 저장
    Task PutAsync(PlayerProfile profile);

    // 랭킹용 전체 조회
    Task<List<PlayerProfile>> GetAllAsync();
}
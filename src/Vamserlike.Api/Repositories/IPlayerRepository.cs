using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public interface IPlayerRepository
{
    // 유저 ID(Cognito sub)로 플레이어 프로필 1건 조회
    Task<PlayerProfile?> GetByUserIdAsync(string userId);

    // 플레이어 프로필 저장(없으면 생성, 있으면 덮어쓰기)
    Task PutAsync(PlayerProfile profile);
}
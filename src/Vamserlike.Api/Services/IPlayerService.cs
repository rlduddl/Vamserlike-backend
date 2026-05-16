using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Game;

namespace Vamserlike.Api.Services;

public interface IPlayerService
{
    // 로그인 후 최초 플레이어 데이터 생성
    Task<PlayerMeResponse> InitAsync(AuthMeResponse currentUser);

    // 내 플레이어 정보 조회
    Task<PlayerMeResponse> GetMeAsync(AuthMeResponse currentUser);

    // 게임 결과 저장
    Task<PlayerMeResponse> UpdateProgressAsync(AuthMeResponse currentUser, UpdateProgressRequest request);

    // 닉네임 저장 또는 수정
    Task<PlayerMeResponse> SetNicknameAsync(AuthMeResponse currentUser, SetNicknameRequest request);

    // 랭킹 조회
    Task<List<RankingItemResponse>> GetRankingAsync(int take);
}
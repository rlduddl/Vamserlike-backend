using Vamserlike.Api.Dtos.Auth;
using Vamserlike.Api.Dtos.Game;

namespace Vamserlike.Api.Services;

public interface IPlayerService
{
    Task<PlayerStateResponse> InitializeMyPlayerAsync(AuthMeResponse currentUser);
    Task<PlayerStateResponse> GetMyStateAsync(AuthMeResponse currentUser);
    Task<PlayerStateResponse> UpdateMyProgressAsync(AuthMeResponse currentUser, UpdateProgressRequest request);
}
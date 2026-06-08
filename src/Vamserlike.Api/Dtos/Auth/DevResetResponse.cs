namespace Vamserlike.Api.Dtos.Auth;

public class DevResetResponse
{
    // 삭제된 Cognito 유저 수
    public int DeletedCognitoUsers { get; set; }

    // 삭제된 DynamoDB 플레이어 수
    public int DeletedDbPlayers { get; set; }
}
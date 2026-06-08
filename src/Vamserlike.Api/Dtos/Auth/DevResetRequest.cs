namespace Vamserlike.Api.Dtos.Auth;

public class DevResetRequest
{
    // 안전 확인 문구
    // DELETE_TEST_DATA 입력해야 실행
    public string ConfirmText { get; set; } = string.Empty;

    // true면 Cognito 유저 전체 삭제
    public bool DeleteAllCognitoUsers { get; set; } = false;
}
namespace Vamserlike.Api.Configurations;

/// <summary>
/// DynamoDB 설정 바인딩 클래스
/// </summary>
public class DynamoDbOptions
{
    /// <summary>
    /// 플레이어 정보를 저장할 DynamoDB 테이블 이름
    /// </summary>
    public string TableName { get; set; } = "VamserlikePlayers";
}
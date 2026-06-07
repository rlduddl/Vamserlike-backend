namespace Vamserlike.Api.Configurations;

// appsettings.json의 "MySql" 섹션을 바인딩하기 위한 설정 클래스
// 예: MySql:ConnectionString 값을 C# 코드에서 Options 패턴으로 사용
public class MySqlOptions
{
    // MySQL 접속 문자열
    public string ConnectionString { get; set; } = "";
}
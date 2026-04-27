namespace Vamserlike.Api.Configurations;

public class CognitoOptions
{
    public string Region { get; set; } = "ap-northeast-2";
    public string UserPoolId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
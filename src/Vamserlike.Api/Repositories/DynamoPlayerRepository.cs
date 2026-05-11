using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

public class DynamoPlayerRepository : IPlayerRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoPlayerRepository(
        IAmazonDynamoDB dynamoDb,
        IOptions<DynamoDbOptions> dynamoOptions)
    {
        _dynamoDb = dynamoDb;
        _tableName = dynamoOptions.Value.TableName;
    }

    // UserId(Cognito sub)로 플레이어 1명 조회
    public async Task<PlayerProfile?> GetByUserIdAsync(string userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["UserId"] = new AttributeValue { S = userId }
            }
        });

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        var item = response.Item;

        return new PlayerProfile
        {
            UserId = item["UserId"].S,
            Email = GetString(item, "Email"),
            Nickname = GetString(item, "Nickname", "guest"),
            SelectedCharacterId = GetString(item, "SelectedCharacterId", "rice_farmer"),
            LastPlayedCharacterId = GetString(item, "LastPlayedCharacterId", "rice_farmer"),
            BestScore = GetInt(item, "BestScore"),
            HighestLevel = GetInt(item, "HighestLevel"),
            TotalPlayCount = GetInt(item, "TotalPlayCount"),
            UpdatedAtUtc = GetDateTime(item, "UpdatedAtUtc")
        };
    }

    // 플레이어 전체 프로필 저장
    public async Task PutAsync(PlayerProfile profile)
    {
        profile.UpdatedAtUtc = DateTime.UtcNow;

        var item = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new AttributeValue { S = profile.UserId },
            ["Email"] = new AttributeValue { S = profile.Email ?? string.Empty },
            ["Nickname"] = new AttributeValue { S = profile.Nickname ?? "guest" },
            ["SelectedCharacterId"] = new AttributeValue { S = profile.SelectedCharacterId ?? "rice_farmer" },
            ["LastPlayedCharacterId"] = new AttributeValue { S = profile.LastPlayedCharacterId ?? "rice_farmer" },
            ["BestScore"] = new AttributeValue { N = profile.BestScore.ToString() },
            ["HighestLevel"] = new AttributeValue { N = profile.HighestLevel.ToString() },
            ["TotalPlayCount"] = new AttributeValue { N = profile.TotalPlayCount.ToString() },
            ["UpdatedAtUtc"] = new AttributeValue { S = profile.UpdatedAtUtc.ToString("O") }
        };

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }

    // 문자열 읽기
    private static string GetString(
        Dictionary<string, AttributeValue> item,
        string key,
        string defaultValue = "")
    {
        if (item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.S))
        {
            return value.S;
        }

        return defaultValue;
    }

    // 숫자 읽기
    private static int GetInt(
        Dictionary<string, AttributeValue> item,
        string key,
        int defaultValue = 0)
    {
        if (item.TryGetValue(key, out var value) && int.TryParse(value.N, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    // 날짜 읽기
    private static DateTime GetDateTime(
        Dictionary<string, AttributeValue> item,
        string key)
    {
        if (item.TryGetValue(key, out var value) &&
            DateTime.TryParse(value.S, out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }
}
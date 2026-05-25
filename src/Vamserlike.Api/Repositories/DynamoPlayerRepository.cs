using System.Text.Json;
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

    // UserId로 플레이어 1명 조회
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

        return FromItem(response.Item);
    }

    // 플레이어 전체 저장
    public async Task PutAsync(PlayerProfile profile)
    {
        profile.UpdatedAtUtc = DateTime.UtcNow;

        var item = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new AttributeValue { S = profile.UserId },
            ["Email"] = new AttributeValue { S = profile.Email ?? string.Empty },
            ["Nickname"] = new AttributeValue { S = profile.Nickname ?? "guest" },
            ["SelectedCharacterId"] = new AttributeValue { S = profile.SelectedCharacterId ?? string.Empty },
            ["LastPlayedCharacterId"] = new AttributeValue { S = profile.LastPlayedCharacterId ?? string.Empty },
            ["Gold"] = new AttributeValue { N = profile.Gold.ToString() },
            ["BestScore"] = new AttributeValue { N = profile.BestScore.ToString() },
            ["HighestLevel"] = new AttributeValue { N = profile.HighestLevel.ToString() },
            ["TotalPlayCount"] = new AttributeValue { N = profile.TotalPlayCount.ToString() },
            ["TotalKillCount"] = new AttributeValue { N = profile.TotalKillCount.ToString() },
            ["UnlockedCharacterIds"] = new AttributeValue
            {
                S = JsonSerializer.Serialize(profile.UnlockedCharacterIds ?? new List<string>())
            },
            ["UpdatedAtUtc"] = new AttributeValue { S = profile.UpdatedAtUtc.ToString("O") }
        };

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        });
    }

    // 랭킹용 전체 조회
    public async Task<List<PlayerProfile>> GetAllAsync()
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName
        });

        return response.Items
            .Select(FromItem)
            .ToList();
    }

    // DynamoDB item -> PlayerProfile 변환
    private static PlayerProfile FromItem(Dictionary<string, AttributeValue> item)
    {
        return new PlayerProfile
        {
            UserId = GetString(item, "UserId"),
            Email = GetString(item, "Email"),
            Nickname = GetString(item, "Nickname", "guest"),
            SelectedCharacterId = GetString(item, "SelectedCharacterId"),
            LastPlayedCharacterId = GetString(item, "LastPlayedCharacterId"),
            Gold = GetInt(item, "Gold"),
            BestScore = GetInt(item, "BestScore"),
            HighestLevel = GetInt(item, "HighestLevel"),
            TotalPlayCount = GetInt(item, "TotalPlayCount"),
            TotalKillCount = GetInt(item, "TotalKillCount"),
            UnlockedCharacterIds = GetStringList(
                item,
                "UnlockedCharacterIds",
                new List<string>()),
            UpdatedAtUtc = GetDateTime(item, "UpdatedAtUtc")
        };
    }

    // 문자열 읽기
    private static string GetString(
        Dictionary<string, AttributeValue> item,
        string key,
        string defaultValue = "")
    {
        if (item.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value.S))
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
        if (item.TryGetValue(key, out var value) &&
            int.TryParse(value.N, out var parsed))
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

    // JSON 문자열 리스트 읽기
    private static List<string> GetStringList(
        Dictionary<string, AttributeValue> item,
        string key,
        List<string> defaultValue)
    {
        if (item.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value.S))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(value.S);

                if (parsed != null)
                {
                    return parsed;
                }
            }
            catch
            {
                // 파싱 실패 시 기본값 사용
            }
        }

        return defaultValue;
    }
}
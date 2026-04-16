using System.Globalization;
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

    public async Task InitializePlayerAsync(string userId, string userName, string email)
    {
        var now = DateTime.UtcNow;

        var profile = await GetProfileAsync(userId);
        var progress = await GetProgressAsync(userId);

        if (profile is null)
        {
            profile = new PlayerProfile
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                Nickname = "guest",
                CreatedAt = now,
                LastLoginAt = now,
                BestScore = 0,
                HighestLevel = 0
            };
        }
        else
        {
            profile.UserName = string.IsNullOrWhiteSpace(profile.UserName) ? userName : profile.UserName;
            profile.Email = string.IsNullOrWhiteSpace(profile.Email) ? email : profile.Email;
            profile.LastLoginAt = now;
        }

        if (progress is null)
        {
            progress = new PlayerProgress
            {
                UserId = userId,
                Gold = 0,
                TotalKills = 0,
                PlayedSeconds = 0,
                UpdatedAt = now
            };
        }

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToProfileItem(profile)
        });

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToProgressItem(progress)
        });
    }

    public async Task<PlayerProfile?> GetProfileAsync(string userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"PLAYER#{userId}" },
                ["SK"] = new AttributeValue { S = "PROFILE" }
            }
        });

        if (response.Item == null || response.Item.Count == 0)
            return null;

        return FromProfileItem(response.Item);
    }

    public async Task<PlayerProgress?> GetProgressAsync(string userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"PLAYER#{userId}" },
                ["SK"] = new AttributeValue { S = "PROGRESS" }
            }
        });

        if (response.Item == null || response.Item.Count == 0)
            return null;

        return FromProgressItem(response.Item);
    }

    public async Task SaveProgressAsync(PlayerProgress progress)
    {
        progress.UpdatedAt = DateTime.UtcNow;

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToProgressItem(progress)
        });
    }

    private static Dictionary<string, AttributeValue> ToProfileItem(PlayerProfile profile)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"PLAYER#{profile.UserId}" },
            ["SK"] = new AttributeValue { S = "PROFILE" },
            ["EntityType"] = new AttributeValue { S = "PlayerProfile" },
            ["UserId"] = new AttributeValue { S = profile.UserId },
            ["UserName"] = new AttributeValue { S = profile.UserName ?? "" },
            ["Email"] = new AttributeValue { S = profile.Email ?? "" },
            ["Nickname"] = new AttributeValue { S = profile.Nickname ?? "guest" },
            ["CreatedAt"] = new AttributeValue { S = profile.CreatedAt.ToString("O") },
            ["LastLoginAt"] = new AttributeValue { S = profile.LastLoginAt.ToString("O") },
            ["BestScore"] = new AttributeValue { N = profile.BestScore.ToString(CultureInfo.InvariantCulture) },
            ["HighestLevel"] = new AttributeValue { N = profile.HighestLevel.ToString(CultureInfo.InvariantCulture) }
        };
    }

    private static Dictionary<string, AttributeValue> ToProgressItem(PlayerProgress progress)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"PLAYER#{progress.UserId}" },
            ["SK"] = new AttributeValue { S = "PROGRESS" },
            ["EntityType"] = new AttributeValue { S = "PlayerProgress" },
            ["UserId"] = new AttributeValue { S = progress.UserId },
            ["Gold"] = new AttributeValue { N = progress.Gold.ToString(CultureInfo.InvariantCulture) },
            ["TotalKills"] = new AttributeValue { N = progress.TotalKills.ToString(CultureInfo.InvariantCulture) },
            ["PlayedSeconds"] = new AttributeValue { N = progress.PlayedSeconds.ToString(CultureInfo.InvariantCulture) },
            ["UpdatedAt"] = new AttributeValue { S = progress.UpdatedAt.ToString("O") }
        };
    }

    private static PlayerProfile FromProfileItem(Dictionary<string, AttributeValue> item)
    {
        return new PlayerProfile
        {
            UserId = GetString(item, "UserId"),
            UserName = GetString(item, "UserName"),
            Email = GetString(item, "Email"),
            Nickname = GetString(item, "Nickname", "guest"),
            CreatedAt = GetDateTime(item, "CreatedAt"),
            LastLoginAt = GetDateTime(item, "LastLoginAt"),
            BestScore = GetInt(item, "BestScore"),
            HighestLevel = GetInt(item, "HighestLevel")
        };
    }

    private static PlayerProgress FromProgressItem(Dictionary<string, AttributeValue> item)
    {
        return new PlayerProgress
        {
            UserId = GetString(item, "UserId"),
            Gold = GetInt(item, "Gold"),
            TotalKills = GetInt(item, "TotalKills"),
            PlayedSeconds = GetInt(item, "PlayedSeconds"),
            UpdatedAt = GetDateTime(item, "UpdatedAt")
        };
    }

    private static string GetString(Dictionary<string, AttributeValue> item, string key, string defaultValue = "")
    {
        if (item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.S))
            return value.S;

        return defaultValue;
    }

    private static int GetInt(Dictionary<string, AttributeValue> item, string key, int defaultValue = 0)
    {
        if (item.TryGetValue(key, out var value) && int.TryParse(value.N, out var parsed))
            return parsed;

        return defaultValue;
    }

    private static DateTime GetDateTime(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var value) &&
            DateTime.TryParse(value.S, null, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return DateTime.UtcNow;
    }
}
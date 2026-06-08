using System.Text.Json;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Models;

namespace Vamserlike.Api.Repositories;

// 기존 DynamoPlayerRepository를 대체하는 MySQL 기반 Repository
// 기존 Service/Controller 코드는 최대한 유지하고, 저장소 구현체만 MySQL로 변경
public class MySqlPlayerRepository : IPlayerRepository
{
    private readonly string _connectionString;

    public MySqlPlayerRepository(IOptions<MySqlOptions> mySqlOptions)
    {
        _connectionString = mySqlOptions.Value.ConnectionString;
    }

    // 유저 ID로 플레이어 1명 조회
    public async Task<PlayerProfile?> GetByUserIdAsync(string userId)
    {
        await EnsureDatabaseAndTableCreatedAsync();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT
                user_id,
                email,
                nickname,
                selected_character_id,
                last_played_character_id,
                gold,
                best_score,
                highest_level,
                total_play_count,
                total_kill_count,
                unlocked_character_ids,
                updated_at_utc
            FROM player_profiles
            WHERE user_id = @user_id
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", userId);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PlayerProfile
        {
            UserId = GetString(reader, "user_id"),
            Email = GetString(reader, "email"),
            Nickname = GetString(reader, "nickname", "guest"),
            SelectedCharacterId = GetString(reader, "selected_character_id"),
            LastPlayedCharacterId = GetString(reader, "last_played_character_id"),
            Gold = GetInt32(reader, "gold"),
            BestScore = GetInt32(reader, "best_score"),
            HighestLevel = GetInt32(reader, "highest_level"),
            TotalPlayCount = GetInt32(reader, "total_play_count"),
            TotalKillCount = GetInt32(reader, "total_kill_count"),
            UnlockedCharacterIds = DeserializeStringList(GetString(reader, "unlocked_character_ids", "[]")),
            UpdatedAtUtc = GetDateTime(reader, "updated_at_utc")
        };
    }

    // 플레이어 전체 저장
    // 기존 DynamoDB PutItem 역할을 MySQL INSERT ... ON DUPLICATE KEY UPDATE로 대체
    public async Task PutAsync(PlayerProfile profile)
    {
        await EnsureDatabaseAndTableCreatedAsync();

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO player_profiles
            (
                user_id,
                email,
                nickname,
                selected_character_id,
                last_played_character_id,
                gold,
                best_score,
                highest_level,
                total_play_count,
                total_kill_count,
                unlocked_character_ids,
                updated_at_utc
            )
            VALUES
            (
                @user_id,
                @email,
                @nickname,
                @selected_character_id,
                @last_played_character_id,
                @gold,
                @best_score,
                @highest_level,
                @total_play_count,
                @total_kill_count,
                @unlocked_character_ids,
                @updated_at_utc
            )
            ON DUPLICATE KEY UPDATE
                email = VALUES(email),
                nickname = VALUES(nickname),
                selected_character_id = VALUES(selected_character_id),
                last_played_character_id = VALUES(last_played_character_id),
                gold = VALUES(gold),
                best_score = VALUES(best_score),
                highest_level = VALUES(highest_level),
                total_play_count = VALUES(total_play_count),
                total_kill_count = VALUES(total_kill_count),
                unlocked_character_ids = VALUES(unlocked_character_ids),
                updated_at_utc = VALUES(updated_at_utc);
            """;

        await using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@user_id", profile.UserId);
        command.Parameters.AddWithValue("@email", profile.Email ?? "");
        command.Parameters.AddWithValue("@nickname", profile.Nickname ?? "guest");
        command.Parameters.AddWithValue("@selected_character_id", profile.SelectedCharacterId ?? "");
        command.Parameters.AddWithValue("@last_played_character_id", profile.LastPlayedCharacterId ?? "");
        command.Parameters.AddWithValue("@gold", profile.Gold);
        command.Parameters.AddWithValue("@best_score", profile.BestScore);
        command.Parameters.AddWithValue("@highest_level", profile.HighestLevel);
        command.Parameters.AddWithValue("@total_play_count", profile.TotalPlayCount);
        command.Parameters.AddWithValue("@total_kill_count", profile.TotalKillCount);
        command.Parameters.AddWithValue("@unlocked_character_ids", SerializeStringList(profile.UnlockedCharacterIds));
        command.Parameters.AddWithValue("@updated_at_utc", profile.UpdatedAtUtc);

        await command.ExecuteNonQueryAsync();
    }

    // 랭킹용 전체 조회
    // 기본 정렬은 최고 점수 내림차순, 누적 처치 수 내림차순
    public async Task<List<PlayerProfile>> GetAllAsync()
    {
        await EnsureDatabaseAndTableCreatedAsync();

        var players = new List<PlayerProfile>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT
                user_id,
                email,
                nickname,
                selected_character_id,
                last_played_character_id,
                gold,
                best_score,
                highest_level,
                total_play_count,
                total_kill_count,
                unlocked_character_ids,
                updated_at_utc
            FROM player_profiles
            ORDER BY best_score DESC, total_kill_count DESC, updated_at_utc ASC;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            players.Add(new PlayerProfile
            {
                UserId = GetString(reader, "user_id"),
                Email = GetString(reader, "email"),
                Nickname = GetString(reader, "nickname", "guest"),
                SelectedCharacterId = GetString(reader, "selected_character_id"),
                LastPlayedCharacterId = GetString(reader, "last_played_character_id"),
                Gold = GetInt32(reader, "gold"),
                BestScore = GetInt32(reader, "best_score"),
                HighestLevel = GetInt32(reader, "highest_level"),
                TotalPlayCount = GetInt32(reader, "total_play_count"),
                TotalKillCount = GetInt32(reader, "total_kill_count"),
                UnlockedCharacterIds = DeserializeStringList(GetString(reader, "unlocked_character_ids", "[]")),
                UpdatedAtUtc = GetDateTime(reader, "updated_at_utc")
            });
        }

        return players;
    }

    // 모든 플레이어 데이터 삭제
    // 테스트 데이터 초기화용 API에서 사용 가능
    public async Task<int> DeleteAllAsync()
    {
        await EnsureDatabaseAndTableCreatedAsync();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            DELETE FROM player_profiles;
            """;

        await using var command = new MySqlCommand(sql, connection);

        return await command.ExecuteNonQueryAsync();
    }

    // DB와 테이블 자동 생성
    // 팀원이 Azure MySQL 또는 AWS RDS MySQL을 만들면 ConnectionString만 바꿔서 연결 가능
    private async Task EnsureDatabaseAndTableCreatedAsync()
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString);

        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "vamserlike";
            builder.Database = databaseName;
        }

        // CREATE DATABASE를 위해 일단 Database 없이 서버에 접속
        var serverConnectionBuilder = new MySqlConnectionStringBuilder(builder.ConnectionString)
        {
            Database = ""
        };

        await using (var serverConnection = new MySqlConnection(serverConnectionBuilder.ConnectionString))
        {
            await serverConnection.OpenAsync();

            var createDatabaseSql = $"""
                CREATE DATABASE IF NOT EXISTS `{databaseName}`
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_unicode_ci;
                """;

            await using var createDatabaseCommand = new MySqlCommand(createDatabaseSql, serverConnection);
            await createDatabaseCommand.ExecuteNonQueryAsync();
        }

        // 실제 DB에 접속해서 테이블 생성
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS player_profiles
            (
                user_id VARCHAR(128) NOT NULL,
                email VARCHAR(320) NOT NULL DEFAULT '',
                nickname VARCHAR(64) NOT NULL DEFAULT 'guest',
                selected_character_id VARCHAR(64) NOT NULL DEFAULT '',
                last_played_character_id VARCHAR(64) NOT NULL DEFAULT '',
                gold INT NOT NULL DEFAULT 0,
                best_score INT NOT NULL DEFAULT 0,
                highest_level INT NOT NULL DEFAULT 0,
                total_play_count INT NOT NULL DEFAULT 0,
                total_kill_count INT NOT NULL DEFAULT 0,
                unlocked_character_ids LONGTEXT NOT NULL,
                updated_at_utc DATETIME(6) NOT NULL,
                PRIMARY KEY (user_id),
                INDEX idx_best_score (best_score),
                INDEX idx_total_kill_count (total_kill_count)
            )
            ENGINE=InnoDB
            DEFAULT CHARSET=utf8mb4
            COLLATE=utf8mb4_unicode_ci;
            """;

        await using var createTableCommand = new MySqlCommand(createTableSql, connection);
        await createTableCommand.ExecuteNonQueryAsync();
    }

    // List<string>을 MySQL LONGTEXT 컬럼에 JSON 문자열로 저장
    private static string SerializeStringList(List<string>? values)
    {
        return JsonSerializer.Serialize(values ?? new List<string>());
    }

    // MySQL LONGTEXT 컬럼의 JSON 문자열을 List<string>으로 복원
    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    // 컬럼명을 기준으로 문자열 값 읽기
    private static string GetString(MySqlDataReader reader, string columnName, string defaultValue = "")
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return defaultValue;
        }

        return reader.GetString(ordinal);
    }

    // 컬럼명을 기준으로 int 값 읽기
    private static int GetInt32(MySqlDataReader reader, string columnName, int defaultValue = 0)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return defaultValue;
        }

        return reader.GetInt32(ordinal);
    }

    // 컬럼명을 기준으로 DateTime 값 읽기
    private static DateTime GetDateTime(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return DateTime.UtcNow;
        }

        return reader.GetDateTime(ordinal);
    }
}
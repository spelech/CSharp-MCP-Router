using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Models;

namespace McpRouter.Infrastructure.Persistence
{
    public class SqliteDialectStrategy : ISqlDialectStrategy
    {
        public string ProviderName => "sqlite";

        public async Task SaveServerAsync(IDbConnection conn, McpServer server)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson)
                VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @SecretMount, @SecretPath, @SecretField, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson)
                ON CONFLICT(Id) DO UPDATE SET
                    DisplayName = @DisplayName,
                    Url = @Url,
                    Enabled = @Enabled,
                    Hidden = @Hidden,
                    Type = @Type,
                    SecretProvider = @SecretProvider,
                    SecretItemKey = @SecretItemKey,
                    SecretMount = @SecretMount,
                    SecretPath = @SecretPath,
                    SecretField = @SecretField,
                    AuthShape = @AuthShape,
                    CustomHeaderName = @CustomHeaderName,
                    Categories = @Categories,
                    ApiKey = @ApiKey,
                    HeadersJson = @HeadersJson;
            ", server);
        }

        public async Task<IEnumerable<AppKey>> GetAppKeysAsync(IDbConnection conn, string? usernameFilter, bool isAdmin, string? currentUser)
        {
            var sql = "SELECT * FROM AppKeys";
            if (!isAdmin) sql += " WHERE Username = @Username";
            else if (!string.IsNullOrEmpty(usernameFilter)) sql += " WHERE Username = @Username";
            return await conn.QueryAsync<AppKey>(sql, new { Username = isAdmin ? usernameFilter : currentUser });
        }

        public async Task SaveAppKeyAsync(IDbConnection conn, AppKey key)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                VALUES (@Id, @Name, @Username, @OwnerSid, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = @Name,
                    Username = @Username,
                    OwnerSid = @OwnerSid,
                    KeyPrefix = @KeyPrefix,
                    EncryptedKey = @EncryptedKey,
                    ScopesJson = @ScopesJson,
                    ExpiresAt = @ExpiresAt;
            ", key);
        }

        public async Task DeleteAppKeyAsync(IDbConnection conn, string id)
        {
            await conn.ExecuteAsync("DELETE FROM AppKeys WHERE Id = @Id", new { Id = id });
        }

        public async Task SaveSecretProviderAsync(IDbConnection conn, SecretProviderDto dto, string? encryptedConfig)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO SecretProviders (ProviderName, DisplayName, EncryptedConfigJson, IsEnabled)
                VALUES (@ProviderName, @DisplayName, @EncryptedConfigJson, @IsEnabled)
                ON CONFLICT(ProviderName) DO UPDATE SET
                    DisplayName = @DisplayName,
                    EncryptedConfigJson = @EncryptedConfigJson,
                    IsEnabled = @IsEnabled;
            ", new { dto.ProviderName, dto.DisplayName, EncryptedConfigJson = encryptedConfig, dto.IsEnabled });
        }

        public async Task SaveAuthProviderAsync(IDbConnection conn, AuthProviderDto dto, string? encryptedConfig)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, EncryptedConfigJson, IsEnabled)
                VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @EncryptedConfigJson, @IsEnabled)
                ON CONFLICT(ProviderName) DO UPDATE SET
                    DisplayName = @DisplayName,
                    UserHeader = @UserHeader,
                    GroupsHeader = @GroupsHeader,
                    EncryptedConfigJson = @EncryptedConfigJson,
                    IsEnabled = @IsEnabled;
            ", new { dto.ProviderName, dto.DisplayName, dto.UserHeader, dto.GroupsHeader, EncryptedConfigJson = encryptedConfig, dto.IsEnabled });
        }
    }
}

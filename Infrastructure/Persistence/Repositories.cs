using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Models;
using McpRouter.Components.Servers;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Infrastructure.Persistence
{
    public interface ISettingRepository
    {
        Task<RouterSettings?> GetSettingsAsync();
        Task SaveSettingsAsync(RouterSettings settings);
    }

    public interface IServerRepository
    {
        Task<IEnumerable<McpServer>> GetServersAsync();
        Task<IEnumerable<McpServer>> GetEnabledServersAsync();
        Task<McpServer?> GetServerByIdAsync(string id);
        Task SaveServerAsync(McpServer server);
        Task DeleteServerAsync(string id);
    }

    public interface IAppKeyRepository
    {
        Task<IEnumerable<AppKey>> GetAppKeysAsync(string? usernameFilter = null, bool isAdmin = false, string? currentUser = null);
        Task<AppKey?> GetAppKeyByIdAsync(string id);
        Task SaveAppKeyAsync(AppKey key);
        Task DeleteAppKeyAsync(string id);
        Task<int> GetTotalActiveKeysAsync();
        Task<int> GetUserActiveKeysAsync(string username);
    }

    public interface ISecretProviderRepository
    {
        Task<IEnumerable<SecretProviderDto>> GetSecretProvidersAsync();
        Task SaveSecretProviderAsync(SecretProviderDto dto);
    }

    public interface IAuthProviderRepository
    {
        Task<IEnumerable<AuthProviderDto>> GetAuthProvidersAsync();
        Task SaveAuthProviderAsync(AuthProviderDto dto);
    }

    public class DatabaseRepository :
        ISettingRepository,
        IServerRepository,
        IAppKeyRepository,
        ISecretProviderRepository,
        IAuthProviderRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration? _config;

        public DatabaseRepository(IDbConnectionFactory dbFactory, IConfiguration? config = null)
        {
            _dbFactory = dbFactory;
            _config = config;
        }

        // ==========================================
        // ISettingRepository
        // ==========================================
        public async Task<RouterSettings?> GetSettingsAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<RouterSettings>("SELECT * FROM Settings WHERE Id = 'default';");
        }

        public async Task SaveSettingsAsync(RouterSettings settings)
        {
            using var conn = _dbFactory.CreateConnection();
            var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Settings WHERE Id = 'default';");
            if (exists == 0)
            {
                const string insertSql = @"
                    INSERT INTO Settings (Id, DashboardTitle, DashboardIcon, EmbeddingProvider, EmbeddingApiUrl, EmbeddingApiKey, EmbeddingApiModel, EmbeddingModelDir, GlobalMaxKeys, UserMaxKeys)
                    VALUES ('default', @DashboardTitle, @DashboardIcon, @EmbeddingProvider, @EmbeddingApiUrl, @EmbeddingApiKey, @EmbeddingApiModel, @EmbeddingModelDir, @GlobalMaxKeys, @UserMaxKeys);";
                await conn.ExecuteAsync(insertSql, settings);
            }
            else
            {
                const string updateSql = @"
                    UPDATE Settings
                    SET DashboardTitle = @DashboardTitle,
                        DashboardIcon = @DashboardIcon,
                        EmbeddingProvider = @EmbeddingProvider,
                        EmbeddingApiUrl = @EmbeddingApiUrl,
                        EmbeddingApiKey = @EmbeddingApiKey,
                        EmbeddingApiModel = @EmbeddingApiModel,
                        EmbeddingModelDir = @EmbeddingModelDir,
                        GlobalMaxKeys = @GlobalMaxKeys,
                        UserMaxKeys = @UserMaxKeys
                    WHERE Id = 'default';";
                await conn.ExecuteAsync(updateSql, settings);
            }
        }

        // ==========================================
        // IServerRepository
        // ==========================================
        public async Task<IEnumerable<McpServer>> GetServersAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryAsync<McpServer>(@"
                SELECT Id, DisplayName, Url, Enabled, Hidden, Type, Categories, SecretProvider,
                       SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape,
                       CustomHeaderName, ApiKey, HeadersJson, AutoDiscovered
                FROM Servers;");
        }

        public async Task<IEnumerable<McpServer>> GetEnabledServersAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1;");
        }

        public async Task<McpServer?> GetServerByIdAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @Id;", new { Id = id });
        }

        public async Task SaveServerAsync(McpServer server)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            if (provider == "sqlite")
            {
                const string sql = @"
                    INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson, AutoDiscovered)
                    VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @SecretMount, @SecretPath, @SecretField, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson, @AutoDiscovered)
                    ON CONFLICT(Id) DO UPDATE SET
                        DisplayName = @DisplayName, Url = @Url, Enabled = @Enabled, Hidden = @Hidden, Type = @Type,
                        SecretProvider = @SecretProvider, SecretItemKey = @SecretItemKey, SecretMount = @SecretMount,
                        SecretPath = @SecretPath, SecretField = @SecretField, AuthShape = @AuthShape,
                        CustomHeaderName = @CustomHeaderName, Categories = @Categories, ApiKey = @ApiKey,
                        HeadersJson = @HeadersJson, AutoDiscovered = @AutoDiscovered;";
                await conn.ExecuteAsync(sql, server);
            }
            else if (provider == "mssql")
            {
                const string sql = @"
                    IF EXISTS (SELECT 1 FROM Servers WHERE Id = @Id)
                    BEGIN
                        UPDATE Servers SET DisplayName = @DisplayName, Url = @Url, Enabled = @Enabled, Hidden = @Hidden, Type = @Type,
                            SecretProvider = @SecretProvider, SecretItemKey = @SecretItemKey, SecretMount = @SecretMount,
                            SecretPath = @SecretPath, SecretField = @SecretField, AuthShape = @AuthShape,
                            CustomHeaderName = @CustomHeaderName, Categories = @Categories, ApiKey = @ApiKey,
                            HeadersJson = @HeadersJson, AutoDiscovered = @AutoDiscovered WHERE Id = @Id;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson, AutoDiscovered)
                        VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @SecretMount, @SecretPath, @SecretField, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson, @AutoDiscovered);
                    END;";
                await conn.ExecuteAsync(sql, server);
            }
            else if (provider == "mysql")
            {
                const string sql = @"
                    INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, SecretMount, SecretPath, SecretField, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson, AutoDiscovered)
                    VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @SecretMount, @SecretPath, @SecretField, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson, @AutoDiscovered)
                    ON DUPLICATE KEY UPDATE
                        DisplayName = @DisplayName, Url = @Url, Enabled = @Enabled, Hidden = @Hidden, Type = @Type,
                        SecretProvider = @SecretProvider, SecretItemKey = @SecretItemKey, SecretMount = @SecretMount,
                        SecretPath = @SecretPath, SecretField = @SecretField, AuthShape = @AuthShape,
                        CustomHeaderName = @CustomHeaderName, Categories = @Categories, ApiKey = @ApiKey,
                        HeadersJson = @HeadersJson, AutoDiscovered = @AutoDiscovered;";
                await conn.ExecuteAsync(sql, server);
            }
        }

        public async Task DeleteServerAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM Servers WHERE Id = @Id;", new { Id = id });
        }

        // ==========================================
        // IAppKeyRepository
        // ==========================================
        public async Task<IEnumerable<AppKey>> GetAppKeysAsync(string? usernameFilter = null, bool isAdmin = false, string? currentUser = null)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            if (provider == "sqlite")
            {
                if (isAdmin)
                {
                    if (!string.IsNullOrEmpty(usernameFilter))
                    {
                        return await conn.QueryAsync<AppKey>("SELECT * FROM AppKeys WHERE Username = @Username ORDER BY CreatedAt DESC;", new { Username = usernameFilter });
                    }
                    return await conn.QueryAsync<AppKey>("SELECT * FROM AppKeys ORDER BY CreatedAt DESC;");
                }
                return await conn.QueryAsync<AppKey>("SELECT * FROM AppKeys WHERE Username = @Username ORDER BY CreatedAt DESC;", new { Username = currentUser });
            }
            else if (provider == "mysql")
            {
                var parameters = new { p_Username = isAdmin ? usernameFilter : currentUser };
                return await conn.QueryAsync<AppKey>(
                    "sp_GetAppKeys",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            else
            {
                var parameters = new { Username = isAdmin ? usernameFilter : currentUser };
                return await conn.QueryAsync<AppKey>(
                    "sp_GetAppKeys",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<AppKey?> GetAppKeyByIdAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AppKey>("SELECT * FROM AppKeys WHERE Id = @Id;", new { Id = id });
        }

        public async Task SaveAppKeyAsync(AppKey key)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            if (provider == "sqlite")
            {
                const string sql = @"
                    INSERT INTO AppKeys (Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                    VALUES (@Id, @Name, @Username, @OwnerSid, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt)
                    ON CONFLICT(Id) DO UPDATE SET
                        Name = @Name, Username = @Username, OwnerSid = @OwnerSid, KeyPrefix = @KeyPrefix,
                        EncryptedKey = @EncryptedKey, ScopesJson = @ScopesJson, ExpiresAt = @ExpiresAt;";
                await conn.ExecuteAsync(sql, key);
            }
            else if (provider == "mysql")
            {
                await conn.ExecuteAsync(
                    "sp_SaveAppKey",
                    new
                    {
                        p_Id = key.Id,
                        p_Name = key.Name,
                        p_Username = key.Username,
                        p_KeyPrefix = key.KeyPrefix,
                        p_EncryptedKey = key.EncryptedKey,
                        p_ScopesJson = key.ScopesJson,
                        p_OwnerSid = key.OwnerSid ?? "",
                        p_ExpiresAt = key.ExpiresAt
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
            else
            {
                await conn.ExecuteAsync(
                    "sp_SaveAppKey",
                    new
                    {
                        key.Id,
                        key.Name,
                        key.Username,
                        key.OwnerSid,
                        key.KeyPrefix,
                        key.EncryptedKey,
                        key.ScopesJson,
                        key.ExpiresAt
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task DeleteAppKeyAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            if (provider == "sqlite")
            {
                await conn.ExecuteAsync("DELETE FROM AppKeys WHERE Id = @Id;", new { Id = id });
            }
            else if (provider == "mysql")
            {
                await conn.ExecuteAsync(
                    "sp_DeleteAppKey",
                    new { p_Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
            else
            {
                await conn.ExecuteAsync(
                    "sp_DeleteAppKey",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<int> GetTotalActiveKeysAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys;");
        }

        public async Task<int> GetUserActiveKeysAsync(string username)
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;", new { Username = username });
        }

        // ==========================================
        // ISecretProviderRepository
        // ==========================================
        public async Task<IEnumerable<SecretProviderDto>> GetSecretProvidersAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            var list = (await conn.QueryAsync<SecretProviderDto>("SELECT ProviderName, DisplayName, EncryptedConfigJson AS ConfigJson, IsEnabled FROM SecretProviders;")).ToList();
            if (_config != null)
            {
                foreach (var item in list)
                {
                    if (!string.IsNullOrEmpty(item.ConfigJson))
                    {
                        if (SymmetricEncryptionHelper.TryDecrypt(item.ConfigJson, _config, out var decrypted))
                        {
                            item.ConfigJson = decrypted;
                        }
                        else
                        {
                            item.IsDecryptionFailed = true;
                        }
                    }
                }
            }
            return list;
        }

        public async Task SaveSecretProviderAsync(SecretProviderDto dto)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            string? configToSave;
            if (dto.IsDecryptionFailed)
            {
                // Preserve the existing encrypted payload to avoid data loss
                configToSave = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM SecretProviders WHERE ProviderName = @ProviderName;", new { dto.ProviderName });
            }
            else
            {
                configToSave = dto.ConfigJson;
                if (!string.IsNullOrEmpty(configToSave) && _config != null)
                {
                    configToSave = SymmetricEncryptionHelper.Encrypt(configToSave, _config);
                }
            }

            var param = new
            {
                dto.ProviderName,
                dto.DisplayName,
                ConfigJson = configToSave,
                dto.IsEnabled
            };

            if (provider == "sqlite")
            {
                const string sql = @"
                    INSERT INTO SecretProviders (ProviderName, DisplayName, EncryptedConfigJson, IsEnabled)
                    VALUES (@ProviderName, @DisplayName, @ConfigJson, @IsEnabled)
                    ON CONFLICT(ProviderName) DO UPDATE SET DisplayName = @DisplayName, EncryptedConfigJson = @ConfigJson, IsEnabled = @IsEnabled;";
                await conn.ExecuteAsync(sql, param);
            }
            else if (provider == "mysql")
            {
                await conn.ExecuteAsync("sp_SaveSecretProvider", new
                {
                    p_ProviderName = dto.ProviderName,
                    p_DisplayName = dto.DisplayName,
                    p_EncryptedConfigJson = configToSave,
                    p_IsEnabled = dto.IsEnabled ? 1 : 0
                }, commandType: CommandType.StoredProcedure);
            }
            else
            {
                await conn.ExecuteAsync("sp_SaveSecretProvider", new
                {
                    dto.ProviderName,
                    dto.DisplayName,
                    EncryptedConfigJson = configToSave,
                    dto.IsEnabled
                }, commandType: CommandType.StoredProcedure);
            }
        }

        // ==========================================
        // IAuthProviderRepository
        // ==========================================
        public async Task<IEnumerable<AuthProviderDto>> GetAuthProvidersAsync()
        {
            using var conn = _dbFactory.CreateConnection();
            var list = (await conn.QueryAsync<AuthProviderDto>("SELECT ProviderName, DisplayName, UserHeader, GroupsHeader, EncryptedConfigJson AS ConfigJson, IsEnabled FROM AuthProviderConfigs;")).ToList();
            if (_config != null)
            {
                foreach (var item in list)
                {
                    if (!string.IsNullOrEmpty(item.ConfigJson))
                    {
                        if (SymmetricEncryptionHelper.TryDecrypt(item.ConfigJson, _config, out var decrypted))
                        {
                            item.ConfigJson = decrypted;
                        }
                        else
                        {
                            item.IsDecryptionFailed = true;
                        }
                    }
                }
            }
            return list;
        }

        public async Task SaveAuthProviderAsync(AuthProviderDto dto)
        {
            using var conn = _dbFactory.CreateConnection();
            var provider = _dbFactory.ProviderName.ToLower();

            string? configToSave;
            if (dto.IsDecryptionFailed)
            {
                // Preserve the existing encrypted payload to avoid data loss
                configToSave = await conn.ExecuteScalarAsync<string>("SELECT EncryptedConfigJson FROM AuthProviderConfigs WHERE ProviderName = @ProviderName;", new { dto.ProviderName });
            }
            else
            {
                configToSave = dto.ConfigJson;
                if (!string.IsNullOrEmpty(configToSave) && _config != null)
                {
                    configToSave = SymmetricEncryptionHelper.Encrypt(configToSave, _config);
                }
            }

            var param = new
            {
                dto.ProviderName,
                dto.DisplayName,
                dto.UserHeader,
                dto.GroupsHeader,
                ConfigJson = configToSave,
                dto.IsEnabled
            };

            if (provider == "sqlite")
            {
                const string sql = @"
                    INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, EncryptedConfigJson, IsEnabled)
                    VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @ConfigJson, @IsEnabled)
                    ON CONFLICT(ProviderName) DO UPDATE SET DisplayName = @DisplayName, UserHeader = @UserHeader, GroupsHeader = @GroupsHeader, EncryptedConfigJson = @ConfigJson, IsEnabled = @IsEnabled;";
                await conn.ExecuteAsync(sql, param);
            }
            else if (provider == "mysql")
            {
                await conn.ExecuteAsync("sp_SaveAuthProvider", new
                {
                    p_ProviderName = dto.ProviderName,
                    p_DisplayName = dto.DisplayName,
                    p_UserHeader = dto.UserHeader,
                    p_GroupsHeader = dto.GroupsHeader,
                    p_EncryptedConfigJson = configToSave,
                    p_IsEnabled = dto.IsEnabled ? 1 : 0
                }, commandType: CommandType.StoredProcedure);
            }
            else
            {
                await conn.ExecuteAsync("sp_SaveAuthProvider", new
                {
                    dto.ProviderName,
                    dto.DisplayName,
                    dto.UserHeader,
                    dto.GroupsHeader,
                    EncryptedConfigJson = configToSave,
                    dto.IsEnabled
                }, commandType: CommandType.StoredProcedure);
            }
        }
    
        public async Task SaveAuthProvidersBatchAsync(IEnumerable<AuthProviderDto> dtos)
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var provider = _dbFactory.ProviderName.ToLower();

                foreach (var dto in dtos)
                {
                    var configToSave = dto.ConfigJson;
                    if (!string.IsNullOrEmpty(configToSave) && _config != null)
                    {
                        configToSave = SymmetricEncryptionHelper.Encrypt(configToSave, _config);
                    }

                    var param = new
                    {
                        dto.ProviderName,
                        dto.DisplayName,
                        dto.UserHeader,
                        dto.GroupsHeader,
                        ConfigJson = configToSave,
                        dto.IsEnabled
                    };

                    if (provider == "sqlite")
                    {
                        const string sql = @"
                            INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, EncryptedConfigJson, IsEnabled)
                            VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @ConfigJson, @IsEnabled)
                            ON CONFLICT(ProviderName) DO UPDATE SET DisplayName = @DisplayName, UserHeader = @UserHeader, GroupsHeader = @GroupsHeader, EncryptedConfigJson = @ConfigJson, IsEnabled = @IsEnabled;";
                        await conn.ExecuteAsync(sql, param, transaction: tx);
                    }
                    else if (provider == "mysql")
                    {
                        await conn.ExecuteAsync("sp_SaveAuthProvider", new
                        {
                            p_ProviderName = dto.ProviderName,
                            p_DisplayName = dto.DisplayName,
                            p_UserHeader = dto.UserHeader,
                            p_GroupsHeader = dto.GroupsHeader,
                            p_EncryptedConfigJson = configToSave,
                            p_IsEnabled = dto.IsEnabled ? 1 : 0
                        }, commandType: System.Data.CommandType.StoredProcedure, transaction: tx);
                    }
                    else
                    {
                        await conn.ExecuteAsync("sp_SaveAuthProvider", new
                        {
                            dto.ProviderName,
                            dto.DisplayName,
                            dto.UserHeader,
                            dto.GroupsHeader,
                            EncryptedConfigJson = configToSave,
                            dto.IsEnabled
                        }, commandType: System.Data.CommandType.StoredProcedure, transaction: tx);
                    }
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using McpRouter.Models;

namespace McpRouter.Services
{
    public interface ICredentialService
    {
        Task<(AppKey AppKey, string PlaintextKey)> CreateCredentialAsync(
            string name,
            string username,
            string ownerSid,
            List<string> scopes,
            int? expiresInDays);

        Task<bool> RevokeCredentialAsync(string id);
    }

    public class CredentialService : ICredentialService
    {
        private readonly IDbConnectionFactory _dbFactory;

        public CredentialService(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<(AppKey AppKey, string PlaintextKey)> CreateCredentialAsync(
            string name,
            string username,
            string ownerSid,
            List<string> scopes,
            int? expiresInDays)
        {
            var scopesList = scopes ?? new List<string> { "all" };
            var scopeSlug = "global";
            if (scopesList.Any(s => s.StartsWith("server:", StringComparison.OrdinalIgnoreCase)))
            {
                scopeSlug = "server";
            }
            else if (scopesList.Any(s => s.StartsWith("group:", StringComparison.OrdinalIgnoreCase)))
            {
                scopeSlug = "group";
            }
            else if (scopesList.Any(s => s.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)))
            {
                scopeSlug = "tool";
            }

            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // Generate high-entropy 128-bit (16 bytes = 32 hex chars) selector and 256-bit (32 bytes = 64 hex chars) secret
                var selectorBytes = new byte[16];
                var secretBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(selectorBytes);
                    rng.GetBytes(secretBytes);
                }
                var selector = Convert.ToHexString(selectorBytes).ToLowerInvariant();
                var secret = Convert.ToHexString(secretBytes).ToLowerInvariant();

                var prefix = $"mcp-{scopeSlug}-{selector}";
                var plaintextKey = $"{prefix}-{secret}";

                // Store a secure one-way SHA-256 hash of the full plaintext key
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plaintextKey));
                var encryptedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                var appKey = new AppKey
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    Username = username,
                    OwnerSid = ownerSid ?? string.Empty,
                    KeyPrefix = prefix,
                    EncryptedKey = encryptedKey,
                    ScopesJson = JsonSerializer.Serialize(scopesList),
                    ExpiresAt = expiresInDays.HasValue ? DateTime.UtcNow.AddDays(expiresInDays.Value) : null,
                    CreatedAt = DateTime.UtcNow
                };

                using var conn = _dbFactory.CreateConnection();

                // Check for selector collision before insertion
                var collision = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM AppKeys WHERE KeyPrefix = @KeyPrefix;",
                    new { KeyPrefix = prefix });
                if (collision > 0)
                {
                    continue; // Retry with fresh random bytes
                }

                try
                {
                    if (_dbFactory.ProviderName == "sqlite")
                    {
                        const string insertSql = @"
                            INSERT INTO AppKeys (Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                            VALUES (@Id, @Name, @Username, @OwnerSid, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt);";
                        await conn.ExecuteAsync(insertSql, appKey);
                    }
                    else if (_dbFactory.ProviderName == "mysql")
                    {
                        await conn.ExecuteAsync(
                            "sp_SaveAppKey",
                            new
                            {
                                p_Id = appKey.Id,
                                p_Name = appKey.Name,
                                p_Username = appKey.Username,
                                p_KeyPrefix = appKey.KeyPrefix,
                                p_EncryptedKey = appKey.EncryptedKey,
                                p_ScopesJson = appKey.ScopesJson,
                                p_OwnerSid = appKey.OwnerSid,
                                p_ExpiresAt = appKey.ExpiresAt
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
                                appKey.Id,
                                appKey.Name,
                                appKey.Username,
                                appKey.OwnerSid,
                                appKey.KeyPrefix,
                                appKey.EncryptedKey,
                                appKey.ScopesJson,
                                appKey.ExpiresAt
                            },
                            commandType: CommandType.StoredProcedure
                        );
                    }

                    return (appKey, plaintextKey);
                }
                catch (Exception) when (attempt < maxRetries - 1)
                {
                    // Retry on race-condition duplicate key conflict
                    continue;
                }
            }

            throw new InvalidOperationException("Failed to generate a unique credential after multiple attempts.");
        }

        public async Task<bool> RevokeCredentialAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();

            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AppKeys WHERE Id = @Id;",
                new { Id = id });
            if (exists == 0)
            {
                return false;
            }

            if (_dbFactory.ProviderName == "sqlite")
            {
                const string deleteSql = "DELETE FROM AppKeys WHERE Id = @Id;";
                await conn.ExecuteAsync(deleteSql, new { Id = id });
            }
            else if (_dbFactory.ProviderName == "mysql")
            {
                await conn.ExecuteAsync(
                    "sp_DeleteAppKey",
                    new { p_Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }
            else
            {
                // For SQL Server, sp_DeleteAppKey uses SET NOCOUNT ON (returning -1 from ExecuteAsync).
                // Verified existence above guarantees accurate deletion status.
                await conn.ExecuteAsync(
                    "sp_DeleteAppKey",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
            }

            return true;
        }
    }
}

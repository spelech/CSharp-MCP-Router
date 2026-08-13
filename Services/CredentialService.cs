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

            // Generate random secure key
            var randomBytes = new byte[24];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomPart = Convert.ToHexString(randomBytes).ToLowerInvariant();
            var plaintextKey = $"mcp-{scopeSlug}-{randomPart}";

            // KeyPrefix is first 16 characters (e.g. "mcp-global-abcde")
            var prefix = plaintextKey.Substring(0, Math.Min(16, plaintextKey.Length));

            // Store a secure one-way hash of the key
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

            if (_dbFactory.ProviderName == "sqlite")
            {
                const string insertSql = @"
                    INSERT INTO AppKeys (Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                    VALUES (@Id, @Name, @Username, @OwnerSid, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt);";
                await conn.ExecuteAsync(insertSql, appKey);
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

        public async Task<bool> RevokeCredentialAsync(string id)
        {
            using var conn = _dbFactory.CreateConnection();

            if (_dbFactory.ProviderName == "sqlite")
            {
                const string deleteSql = "DELETE FROM AppKeys WHERE Id = @Id;";
                var affected = await conn.ExecuteAsync(deleteSql, new { Id = id });
                return affected > 0;
            }
            else
            {
                var affected = await conn.ExecuteAsync(
                    "sp_DeleteAppKey",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );
                return affected > 0;
            }
        }
    }
}

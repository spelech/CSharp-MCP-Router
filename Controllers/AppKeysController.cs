using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppKeysController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;

        public AppKeysController(IDbConnectionFactory dbFactory, IConfiguration config)
        {
            _dbFactory = dbFactory;
            _config = config;
        }

        private string GetAuthenticatedUser()
        {
            var user = HttpContext.Items["AuthenticatedUser"] as string;
            if (string.IsNullOrEmpty(user))
            {
                user = HttpContext.Request.Headers["Remote-User"].FirstOrDefault()
                    ?? HttpContext.Request.Headers["X-Forwarded-User"].FirstOrDefault()
                    ?? "admin"; // Default fallback
            }
            return user;
        }

        private bool IsAdmin(string username)
        {
            return username.Equals("admin", StringComparison.OrdinalIgnoreCase) || username.Equals("system", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        public async Task<IActionResult> GetAppKeys([FromQuery] string? usernameFilter = null)
        {
            var currentUser = GetAuthenticatedUser();
            using var conn = _dbFactory.CreateConnection();

            try
            {
                IEnumerable<AppKey> keys;

                if (_dbFactory.ProviderName == "sqlite")
                {
                    if (IsAdmin(currentUser))
                    {
                        if (!string.IsNullOrEmpty(usernameFilter))
                        {
                            const string sql = "SELECT * FROM AppKeys WHERE Username = @Username ORDER BY CreatedAt DESC;";
                            keys = await conn.QueryAsync<AppKey>(sql, new { Username = usernameFilter });
                        }
                        else
                        {
                            const string sql = "SELECT * FROM AppKeys ORDER BY CreatedAt DESC;";
                            keys = await conn.QueryAsync<AppKey>(sql);
                        }
                    }
                    else
                    {
                        const string sql = "SELECT * FROM AppKeys WHERE Username = @Username ORDER BY CreatedAt DESC;";
                        keys = await conn.QueryAsync<AppKey>(sql, new { Username = currentUser });
                    }
                }
                else
                {
                    // Use Stored Procedure for MS SQL & MySQL
                    var parameters = new { Username = IsAdmin(currentUser) ? usernameFilter : currentUser };
                    keys = await conn.QueryAsync<AppKey>(
                        "sp_GetAppKeys",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );
                }

                // Return sanitized keys (do not return the EncryptedKey string to prevent exposing DB cipher blocks)
                var result = keys.Select(k => new
                {
                    k.Id,
                    k.Name,
                    k.Username,
                    k.KeyPrefix,
                    Scopes = DeserializeScopes(k.ScopesJson),
                    k.ExpiresAt,
                    k.CreatedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("limits")]
        public async Task<IActionResult> GetAppKeysLimits()
        {
            var currentUser = GetAuthenticatedUser();
            using var conn = _dbFactory.CreateConnection();

            try
            {
                int globalMax = 100;
                int userMax = 5;

                // Load active settings
                const string settingsSql = "SELECT GlobalMaxKeys, UserMaxKeys FROM Settings LIMIT 1;";
                var settings = await conn.QueryFirstOrDefaultAsync<dynamic>(settingsSql);
                if (settings != null)
                {
                    globalMax = settings.GlobalMaxKeys ?? 100;
                    userMax = settings.UserMaxKeys ?? 5;
                }

                int totalActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys;");
                int userActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;", new { Username = currentUser });

                return Ok(new
                {
                    globalMax,
                    userMax,
                    totalActiveKeys,
                    userActiveKeys,
                    isLimitReached = !IsAdmin(currentUser) && (totalActiveKeys >= globalMax || userActiveKeys >= userMax)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppKey([FromBody] CreateAppKeyRequest model)
        {
            var currentUser = GetAuthenticatedUser();

            // Allow admins to generate key for another user, default to current user
            var targetUser = IsAdmin(currentUser) && !string.IsNullOrEmpty(model.Username) ? model.Username : currentUser;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { error = "Name is required." });
            }

            using var conn = _dbFactory.CreateConnection();

            try
            {
                // Retrieve max key limits
                int globalMax = 100;
                int userMax = 5;

                const string settingsSql = "SELECT GlobalMaxKeys, UserMaxKeys FROM Settings LIMIT 1;";
                var settings = await conn.QueryFirstOrDefaultAsync<dynamic>(settingsSql);
                if (settings != null)
                {
                    globalMax = settings.GlobalMaxKeys ?? 100;
                    userMax = settings.UserMaxKeys ?? 5;
                }

                // Check limits (admins bypass limits)
                if (!IsAdmin(currentUser))
                {
                    int totalActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys;");
                    if (totalActiveKeys >= globalMax)
                    {
                        return BadRequest(new { error = $"Global app-key limit of {globalMax} has been reached. Please contact an administrator." });
                    }

                    int userActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;", new { Username = targetUser });
                    if (userActiveKeys >= userMax)
                    {
                        return BadRequest(new { error = $"You have reached your personal app-key limit of {userMax}. Please revoke an existing key first." });
                    }
                }

                // Derive scope slug for key formatting
                var scopes = model.Scopes ?? new List<string> { "all" };
                var scopeSlug = "global";
                if (scopes.Any(s => s.StartsWith("server:", StringComparison.OrdinalIgnoreCase)))
                {
                    scopeSlug = "server";
                }
                else if (scopes.Any(s => s.StartsWith("group:", StringComparison.OrdinalIgnoreCase)))
                {
                    scopeSlug = "group";
                }
                else if (scopes.Any(s => s.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)))
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
                var prefix = plaintextKey.Substring(0, 16);

                // Encrypt the key
                var encryptedKey = SymmetricEncryptionHelper.Encrypt(plaintextKey, _config);

                var appKey = new AppKey
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = model.Name,
                    Username = targetUser,
                    KeyPrefix = prefix,
                    EncryptedKey = encryptedKey,
                    ScopesJson = JsonSerializer.Serialize(scopes),
                    ExpiresAt = model.ExpiresInDays.HasValue ? DateTime.UtcNow.AddDays(model.ExpiresInDays.Value) : null
                };

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string insertSql = @"
                        INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt)
                        VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson, @ExpiresAt, @CreatedAt);";
                    await conn.ExecuteAsync(insertSql, appKey);
                }
                else
                {
                    // Call MS SQL / MySQL stored procedure
                    await conn.ExecuteAsync(
                        "sp_SaveAppKey",
                        new
                        {
                            appKey.Id,
                            appKey.Name,
                            appKey.Username,
                            appKey.KeyPrefix,
                            appKey.EncryptedKey,
                            appKey.ScopesJson,
                            appKey.ExpiresAt
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }

                // Return plaintext key ONCE to the user
                return Ok(new
                {
                    appKey.Id,
                    appKey.Name,
                    appKey.Username,
                    appKey.KeyPrefix,
                    PlaintextKey = plaintextKey,
                    Scopes = scopes,
                    appKey.ExpiresAt,
                    appKey.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RevokeAppKey(string id)
        {
            var currentUser = GetAuthenticatedUser();
            using var conn = _dbFactory.CreateConnection();

            try
            {
                // Look up key to verify ownership or admin rights
                AppKey? appKey = null;
                const string selectSql = "SELECT * FROM AppKeys WHERE Id = @Id;";
                appKey = await conn.QueryFirstOrDefaultAsync<AppKey>(selectSql, new { Id = id });

                if (appKey == null)
                {
                    return NotFound(new { error = "AppKey not found." });
                }

                if (!IsAdmin(currentUser) && !appKey.Username.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string deleteSql = "DELETE FROM AppKeys WHERE Id = @Id;";
                    await conn.ExecuteAsync(deleteSql, new { Id = id });
                }
                else
                {
                    await conn.ExecuteAsync(
                        "sp_DeleteAppKey",
                        new { Id = id },
                        commandType: CommandType.StoredProcedure
                    );
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private List<string> DeserializeScopes(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string> { json };
            }
        }
    }

    public class CreateAppKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Username { get; set; } // Admins can assign to other users
        public List<string>? Scopes { get; set; }
        public int? ExpiresInDays { get; set; }
    }
}

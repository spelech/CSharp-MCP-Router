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
using McpRouter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AppKeysController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;
        private readonly McpRouter.Core.Logging.IAuditLogger _auditLogger;
        private readonly ICredentialService _credentialService;

        public AppKeysController(IDbConnectionFactory dbFactory, IConfiguration config, McpRouter.Core.Logging.IAuditLogger auditLogger, ICredentialService credentialService)
        {
            _dbFactory = dbFactory;
            _config = config;
            _auditLogger = auditLogger;
            _credentialService = credentialService;
        }

        private async Task<McpRouter.Core.Identity.UserIdentityContext> GetIdentityAsync()
        {
            var compositeProvider = HttpContext.RequestServices.GetRequiredService<McpRouter.Core.Identity.CompositeIdentityProvider>();
            return await compositeProvider.ResolveIdentityAsync(HttpContext);
        }

        private bool IsAdmin(McpRouter.Core.Identity.UserIdentityContext identity)
        {
            return McpRouter.Core.Security.SecurityValidationHelper.IsAdmin(identity, _config);
        }

        [HttpGet]
        public async Task<IActionResult> GetAppKeys([FromQuery] string? usernameFilter = null)
        {
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;
            var isAdmin = IsAdmin(identity);
            using var conn = _dbFactory.CreateConnection();

            try
            {
                IEnumerable<AppKey> keys;

                if (_dbFactory.ProviderName == "sqlite")
                {
                    if (isAdmin)
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
                else if (_dbFactory.ProviderName == "mysql")
                {
                    keys = await conn.QueryAsync<AppKey>(
                        "sp_GetAppKeys",
                        new { p_Username = isAdmin ? usernameFilter : currentUser },
                        commandType: CommandType.StoredProcedure
                    );
                }
                else
                {
                    // Use Stored Procedure for MS SQL
                    var parameters = new { Username = isAdmin ? usernameFilter : currentUser };
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
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;
            var isAdmin = IsAdmin(identity);
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
                    var dict = (IDictionary<string, object>)settings;
                    if (dict.TryGetValue("GlobalMaxKeys", out var g) && g != null) globalMax = Convert.ToInt32(g);
                    if (dict.TryGetValue("UserMaxKeys", out var u) && u != null) userMax = Convert.ToInt32(u);
                }

                int totalActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys;");
                int userActiveKeys = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AppKeys WHERE Username = @Username;", new { Username = currentUser });

                return Ok(new
                {
                    globalMax,
                    userMax,
                    totalActiveKeys,
                    userActiveKeys,
                    isLimitReached = !isAdmin && (totalActiveKeys >= globalMax || userActiveKeys >= userMax)
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
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;
            var isAdmin = IsAdmin(identity);
            var ownerSid = identity.AllSids.FirstOrDefault() ?? "";

            // Allow admins to generate key for another user, default to current user
            var targetUser = isAdmin && !string.IsNullOrEmpty(model.Username) ? model.Username : currentUser;

            if (!targetUser.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
            {
                ownerSid = ""; // Decouple admin's SID from target user's key
                var ldapService = HttpContext.RequestServices.GetService<McpRouter.Core.Identity.ILdapService>();
                if (ldapService != null)
                {
                    var targetSids = await ldapService.ResolveUserSidsAsync(targetUser);
                    var primarySid = targetSids.FirstOrDefault();
                    if (!string.IsNullOrEmpty(primarySid))
                    {
                        ownerSid = primarySid;
                    }
                }
            }

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
                    var dict = (IDictionary<string, object>)settings;
                    if (dict.TryGetValue("GlobalMaxKeys", out var g) && g != null) globalMax = Convert.ToInt32(g);
                    if (dict.TryGetValue("UserMaxKeys", out var u) && u != null) userMax = Convert.ToInt32(u);
                }

                // Check limits (admins bypass limits)
                if (!isAdmin)
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

                var scopes = model.Scopes ?? new List<string> { "all" };
                var (appKey, plaintextKey) = await _credentialService.CreateCredentialAsync(
                    model.Name,
                    targetUser,
                    ownerSid,
                    scopes,
                    model.ExpiresInDays
                );

                await _auditLogger.LogAdminActionAsync(
                    identity.Username, "appkey.create", appKey.Id,
                    $"name={appKey.Name};owner={targetUser};ownerSid={ownerSid}", true);

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
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;
            var isAdmin = IsAdmin(identity);
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

                if (!isAdmin && !appKey.Username.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                await _credentialService.RevokeCredentialAsync(id);

                await _auditLogger.LogAdminActionAsync(
                    identity.Username, "appkey.revoke", id,
                    $"owner={appKey.Username}", true);

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

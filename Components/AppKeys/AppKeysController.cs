using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Infrastructure.Logging;
using McpRouter.Infrastructure.Identity;
using McpRouter.Components.Clients;
using McpRouter.Components.Authorization;
using McpRouter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Dapper;

namespace McpRouter.Components.AppKeys
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AppKeysController : ControllerBase
    {
        private readonly IAppKeyRepository _appKeyRepository;
        private readonly ISettingRepository _settingRepository;
        private readonly IConfiguration _config;
        private readonly IAuditLogger _auditLogger;
        private readonly ICredentialService _credentialService;

        public AppKeysController(
            IAppKeyRepository appKeyRepository,
            ISettingRepository settingRepository,
            IConfiguration config,
            IAuditLogger auditLogger,
            ICredentialService credentialService)
        {
            _appKeyRepository = appKeyRepository;
            _settingRepository = settingRepository;
            _config = config;
            _auditLogger = auditLogger;
            _credentialService = credentialService;
        }

        private async Task<UserIdentityContext> GetIdentityAsync()
        {
            var compositeProvider = HttpContext.RequestServices.GetRequiredService<CompositeIdentityProvider>();
            return await compositeProvider.ResolveIdentityAsync(HttpContext);
        }

        private bool IsAdmin(UserIdentityContext identity)
        {
            return SecurityValidationHelper.IsAdmin(identity, _config);
        }

        [HttpGet]
        public async Task<IActionResult> GetAppKeys([FromQuery] string? usernameFilter = null)
        {
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;
            var isAdmin = IsAdmin(identity);

            try
            {
                var keys = await _appKeyRepository.GetAppKeysAsync(usernameFilter, isAdmin, currentUser);

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

            try
            {
                int globalMax = 0;
                int userMax = 0;

                // Load active settings
                var settings = await _settingRepository.GetSettingsAsync();
                if (settings != null)
                {
                    globalMax = settings.GlobalMaxKeys;
                    userMax = settings.UserMaxKeys;
                }

                int totalActiveKeys = await _appKeyRepository.GetTotalActiveKeysAsync();
                int userActiveKeys = await _appKeyRepository.GetUserActiveKeysAsync(currentUser);

                return Ok(new
                {
                    globalMax,
                    userMax,
                    totalActiveKeys,
                    userActiveKeys,
                    isLimitReached = !isAdmin && ((globalMax > 0 && totalActiveKeys >= globalMax) || (userMax > 0 && userActiveKeys >= userMax))
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
                var ldapService = HttpContext.RequestServices.GetService<ILdapService>();
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

            try
            {
                // Retrieve max key limits (0 = unlimited)
                int globalMax = 0;
                int userMax = 0;

                var settings = await _settingRepository.GetSettingsAsync();
                if (settings != null)
                {
                    globalMax = settings.GlobalMaxKeys;
                    userMax = settings.UserMaxKeys;
                }

                // Check limits (admins bypass limits; 0 = unlimited)
                if (!isAdmin)
                {
                    if (globalMax > 0)
                    {
                        int totalActiveKeys = await _appKeyRepository.GetTotalActiveKeysAsync();
                        if (totalActiveKeys >= globalMax)
                        {
                            return BadRequest(new { error = $"Global app-key limit of {globalMax} has been reached. Please contact an administrator." });
                        }
                    }

                    if (userMax > 0)
                    {
                        int userActiveKeys = await _appKeyRepository.GetUserActiveKeysAsync(targetUser);
                        if (userActiveKeys >= userMax)
                        {
                            return BadRequest(new { error = $"You have reached your personal app-key limit of {userMax}. Please revoke an existing key first." });
                        }
                    }
                }

                var scopes = model.Scopes ?? new List<string> { "all" };

                // Validate category scopes
                foreach (var scope in scopes)
                {
                    if (string.IsNullOrWhiteSpace(scope)) continue;
                    var trimmed = scope.Trim();
                    if (trimmed.StartsWith("category:", StringComparison.OrdinalIgnoreCase))
                    {
                        var catName = trimmed.Substring("category:".Length).Trim();
                        if (string.IsNullOrWhiteSpace(catName))
                        {
                            return BadRequest(new { error = "Category scope cannot be empty." });
                        }

                        if (!isAdmin)
                        {
                            var registeredCategories = await GetRegisteredCategoriesAsync();
                            if (!registeredCategories.Any(c => string.Equals(c, catName, StringComparison.OrdinalIgnoreCase)))
                            {
                                return BadRequest(new { error = $"Category '{catName}' does not exist among registered servers." });
                            }
                        }
                    }
                }

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

            try
            {
                var appKey = await _appKeyRepository.GetAppKeyByIdAsync(id);

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

        private async Task<List<string>> GetRegisteredCategoriesAsync()
        {
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dbFactory = HttpContext.RequestServices.GetService<IDbConnectionFactory>();
            if (dbFactory != null)
            {
                try
                {
                    using var conn = dbFactory.CreateConnection();
                    var rawList = await conn.QueryAsync<string>("SELECT Categories FROM Servers WHERE Enabled = 1");
                    foreach (var rawCat in rawList)
                    {
                        if (string.IsNullOrWhiteSpace(rawCat)) continue;
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<string>>(rawCat);
                            if (list != null)
                            {
                                foreach (var c in list)
                                {
                                    if (!string.IsNullOrWhiteSpace(c)) categories.Add(c.Trim());
                                }
                            }
                        }
                        catch
                        {
                            var parts = rawCat.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var p in parts) categories.Add(p);
                        }
                    }
                }
                catch
                {
                    // If Servers table does not exist or DB error
                }
            }
            return categories.ToList();
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
}




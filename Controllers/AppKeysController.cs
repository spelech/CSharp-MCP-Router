using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Core.Database;
using McpRouter.Core.Secrets;
using McpRouter.Models;
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
        private readonly IAppKeyRepository _appKeyRepository;
        private readonly ISettingRepository _settingRepository;
        private readonly IConfiguration _config;
        private readonly McpRouter.Core.Logging.IAuditLogger _auditLogger;

        public AppKeysController(
            IAppKeyRepository appKeyRepository,
            ISettingRepository settingRepository,
            IConfiguration config,
            McpRouter.Core.Logging.IAuditLogger auditLogger)
        {
            _appKeyRepository = appKeyRepository;
            _settingRepository = settingRepository;
            _config = config;
            _auditLogger = auditLogger;
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
                int globalMax = 100;
                int userMax = 5;

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

            try
            {
                // Retrieve max key limits
                int globalMax = 100;
                int userMax = 5;

                var settings = await _settingRepository.GetSettingsAsync();
                if (settings != null)
                {
                    globalMax = settings.GlobalMaxKeys;
                    userMax = settings.UserMaxKeys;
                }

                // Check limits (admins bypass limits)
                if (!isAdmin)
                {
                    int totalActiveKeys = await _appKeyRepository.GetTotalActiveKeysAsync();
                    if (totalActiveKeys >= globalMax)
                    {
                        return BadRequest(new { error = $"Global app-key limit of {globalMax} has been reached. Please contact an administrator." });
                    }

                    int userActiveKeys = await _appKeyRepository.GetUserActiveKeysAsync(targetUser);
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

                // Store a secure one-way hash of the key
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plaintextKey));
                var encryptedKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                var appKey = new AppKey
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = model.Name,
                    Username = targetUser,
                    OwnerSid = ownerSid,
                    KeyPrefix = prefix,
                    EncryptedKey = encryptedKey,
                    ScopesJson = JsonSerializer.Serialize(scopes),
                    ExpiresAt = model.ExpiresInDays.HasValue ? DateTime.UtcNow.AddDays(model.ExpiresInDays.Value) : null,
                    CreatedAt = DateTime.UtcNow
                };

                await _appKeyRepository.SaveAppKeyAsync(appKey);

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

                await _appKeyRepository.DeleteAppKeyAsync(id);

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

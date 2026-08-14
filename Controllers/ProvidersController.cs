using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpRouter.Core.Database;
using McpRouter.Core.Identity;
using McpRouter.Core.Logging;
using McpRouter.Core.Secrets;
using McpRouter.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace McpRouter.Controllers
{
    public class SecretProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ConfigJson { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class AuthProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserHeader { get; set; } = "Remote-User";
        public string GroupsHeader { get; set; } = "Remote-Groups";
        public string? ConfigJson { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class ProvidersController : ControllerBase
    {
        private readonly ISecretProviderRepository _secretRepo;
        private readonly IAuthProviderRepository _authRepo;

        public ProvidersController(
            ISecretProviderRepository secretRepo,
            IAuthProviderRepository authRepo)
        {
            _secretRepo = secretRepo;
            _authRepo = authRepo;
        }

        [HttpGet("")]
        [HttpGet("/api/admin/providers")]
        public async Task<IActionResult> GetAllProviders()
        {
            try
            {
                var secretProviders = (await _secretRepo.GetSecretProvidersAsync()).ToList();
                foreach (var p in secretProviders)
                {
                    p.ConfigJson = ProviderConfigSecurityHelper.RedactConfigJson(p.ConfigJson);
                }

                var authProviders = (await _authRepo.GetAuthProvidersAsync()).ToList();
                foreach (var p in authProviders)
                {
                    p.ConfigJson = ProviderConfigSecurityHelper.RedactConfigJson(p.ConfigJson);
                }

                return Ok(new
                {
                    secretProviders,
                    authProviders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("secrets")]
        [HttpGet("secret")]
        public async Task<IActionResult> GetSecretProviders()
        {
            try
            {
                var providers = (await _secretRepo.GetSecretProvidersAsync()).ToList();
                foreach (var p in providers)
                {
                    p.ConfigJson = ProviderConfigSecurityHelper.RedactConfigJson(p.ConfigJson);
                }
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("secrets")]
        [HttpPost("secret")]
        public async Task<IActionResult> SaveSecretProvider(
            [FromBody] SecretProviderDto dto,
            [FromServices] IAuditLogger auditLogger,
            [FromServices] IServiceProvider? serviceProvider = null)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            var username = User?.Identity?.Name ?? "anonymous";
            try
            {
                ProviderConfigSecurityHelper.ValidateSecretProviderConfig(dto);

                // Merge masked asterisks with existing decrypted config if updating existing provider
                var existingProviders = await _secretRepo.GetSecretProvidersAsync();
                var existing = existingProviders?.FirstOrDefault(p =>
                    string.Equals(p.ProviderName, dto.ProviderName, StringComparison.OrdinalIgnoreCase));
                if (existing != null && !string.IsNullOrEmpty(existing.ConfigJson))
                {
                    dto.ConfigJson = ProviderConfigSecurityHelper.MergeWithExistingConfig(dto.ConfigJson, existing.ConfigJson);
                }

                await _secretRepo.SaveSecretProviderAsync(dto);

                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, redactedDetails ?? "", true);

                // Dynamic runtime reload
                var sp = serviceProvider ?? HttpContext?.RequestServices;
                if (sp != null)
                {
                    var retrievers = sp.GetServices<ISecretRetriever>();
                    foreach (var retriever in retrievers.OfType<VaultSecretRetriever>())
                    {
                        await retriever.ReloadConfigAsync();
                    }
                }

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuthProviders()
        {
            try
            {
                var providers = (await _authRepo.GetAuthProvidersAsync()).ToList();
                foreach (var p in providers)
                {
                    p.ConfigJson = ProviderConfigSecurityHelper.RedactConfigJson(p.ConfigJson);
                }
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("auth")]
        public async Task<IActionResult> SaveAuthProvider(
            [FromBody] AuthProviderDto dto,
            [FromServices] IAuditLogger auditLogger,
            [FromServices] IServiceProvider? serviceProvider = null)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            var username = User?.Identity?.Name ?? "anonymous";
            try
            {
                ProviderConfigSecurityHelper.ValidateAuthProviderConfig(dto);

                // Merge masked asterisks with existing decrypted config if updating existing provider
                var existingProviders = await _authRepo.GetAuthProvidersAsync();
                var existing = existingProviders?.FirstOrDefault(p =>
                    string.Equals(p.ProviderName, dto.ProviderName, StringComparison.OrdinalIgnoreCase));
                if (existing != null && !string.IsNullOrEmpty(existing.ConfigJson))
                {
                    dto.ConfigJson = ProviderConfigSecurityHelper.MergeWithExistingConfig(dto.ConfigJson, existing.ConfigJson);
                }

                await _authRepo.SaveAuthProviderAsync(dto);

                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, redactedDetails ?? "", true);

                // Dynamic runtime reload
                var sp = serviceProvider ?? HttpContext?.RequestServices;
                if (sp != null)
                {
                    var ldapService = sp.GetService<ILdapService>();
                    if (ldapService is LdapActiveDirectoryService ldapAd)
                    {
                        ldapAd.Reload();
                    }
                }

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

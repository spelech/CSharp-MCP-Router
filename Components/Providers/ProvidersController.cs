using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Identity;
using McpRouter.Infrastructure.Logging;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Components.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace McpRouter.Components.Providers
{
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
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
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
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
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
                return BadRequest(new { error = "A validation error occurred." });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost("secrets/test-vault")]
        [HttpPost("/api/settings/secrets/test-vault")]
        public async Task<IActionResult> TestVaultConnection([FromBody] TestVaultRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new { success = false, error = "Vault Address is required." });
            }

            if (request.Address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(request.Address, UriKind.Absolute, out var uri))
                {
                    bool isLocalhost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
                                       uri.Host.Equals("127.0.0.1") || 
                                       uri.Host.Equals("::1");
                    bool isSimpleHost = !uri.Host.Contains('.');

                    if (!isLocalhost && !isSimpleHost && !McpRouter.Components.Authorization.SecurityValidationHelper.IsPrivateOrLoopback(request.Address))
                    {
                        return BadRequest(new { success = false, error = "Vault Address must use the HTTPS scheme in production for non-local addresses." });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, error = "Invalid Vault Address format." });
                }
            }

            try
            {
                VaultSharp.V1.AuthMethods.IAuthMethodInfo authMethod;
                if (string.Equals(request.AuthMethod, "approle", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(request.RoleId) && !string.IsNullOrEmpty(request.SecretId)))
                {
                    if (string.IsNullOrEmpty(request.RoleId) || string.IsNullOrEmpty(request.SecretId))
                    {
                        return BadRequest(new { success = false, error = "Vault AppRole requires both RoleId and SecretId." });
                    }
                    authMethod = new VaultSharp.V1.AuthMethods.AppRole.AppRoleAuthMethodInfo(request.RoleId, request.SecretId);
                }
                else
                {
                    if (string.IsNullOrEmpty(request.Token))
                    {
                        return BadRequest(new { success = false, error = "Vault Token is required for token authentication." });
                    }
                    authMethod = new VaultSharp.V1.AuthMethods.Token.TokenAuthMethodInfo(request.Token);
                }

                var settings = new VaultSharp.VaultClientSettings(request.Address, authMethod);
                var client = new VaultSharp.VaultClient(settings);
                var tokenInfo = await client.V1.Auth.Token.LookupSelfAsync();

                return Ok(new { success = true, message = $"Vault authentication successful. Token TTL: {tokenInfo?.Data?.TimeToLive ?? 0}s." });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return Ok(new { success = false, error = "Vault connection failed." });
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
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
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
                return BadRequest(new { error = "A validation error occurred." });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, redactedDetails ?? "", false, ex.Message);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
        [HttpPost("auth/batch")]
        public async Task<IActionResult> SaveAuthProvidersBatch(
            [FromBody] IEnumerable<AuthProviderDto> dtos,
            [FromServices] IAuditLogger auditLogger,
            [FromServices] IServiceProvider? serviceProvider = null)
        {
            var dtoList = dtos.ToList();
            if (!dtoList.Any()) return BadRequest(new { error = "No providers provided" });

            if (!dtoList.Any(p => p.IsEnabled))
            {
                return BadRequest(new { error = "Cannot disable all authentication providers simultaneously. At least one must be enabled." });
            }

            var username = User?.Identity?.Name ?? "anonymous";
            try
            {
                var existingProviders = (await _authRepo.GetAuthProvidersAsync()).ToList();

                foreach (var dto in dtoList)
                {
                    if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });
                    ProviderConfigSecurityHelper.ValidateAuthProviderConfig(dto);
                    
                    var existing = existingProviders.FirstOrDefault(p =>
                        string.Equals(p.ProviderName, dto.ProviderName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null && !string.IsNullOrEmpty(existing.ConfigJson))
                    {
                        dto.ConfigJson = ProviderConfigSecurityHelper.MergeWithExistingConfig(dto.ConfigJson, existing.ConfigJson);
                    }
                }

                if (_authRepo is DatabaseRepository dbRepo)
                {
                    await dbRepo.SaveAuthProvidersBatchAsync(dtoList);
                }

                foreach (var dto in dtoList)
                {
                    var redactedDetails = ProviderConfigSecurityHelper.RedactConfigJson(dto.ConfigJson);
                    _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvidersBatch", dto.ProviderName, redactedDetails ?? "", true);
                }

                var sp = serviceProvider ?? HttpContext?.RequestServices;
                if (sp != null)
                {
                    var ldapService = sp.GetService<McpRouter.Infrastructure.Identity.ILdapService>();
                    if (ldapService is McpRouter.Infrastructure.Identity.LdapActiveDirectoryService ldapAd)
                    {
                        ldapAd.Reload();
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvidersBatch", "batch", "", false, ex.Message);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost("auth/test-ad")]
        [HttpPost("/api/settings/auth/test-ad")]
        public async Task<IActionResult> TestLdapConnection([FromBody] TestLdapRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Server))
            {
                return BadRequest(new { success = false, error = "LDAP Server is required." });
            }

            int port = request.Port ?? 636;
            bool useSsl = request.UseSsl ?? (port == 636);

            if (port == 389 && !useSsl)
            {
                return BadRequest(new { success = false, error = "LDAP over plaintext (port 389) is disabled for security. Use LDAPS port 636 or set useSsl=true." });
            }

            try
            {
                var identifier = new System.DirectoryServices.Protocols.LdapDirectoryIdentifier(request.Server, port);
                System.Net.NetworkCredential? credential = null;
                if (!string.IsNullOrEmpty(request.BindDn) && !string.IsNullOrEmpty(request.BindPassword))
                {
                    credential = new System.Net.NetworkCredential(request.BindDn, request.BindPassword);
                }

                using var connection = new System.DirectoryServices.Protocols.LdapConnection(identifier, credential, System.DirectoryServices.Protocols.AuthType.Basic);
                connection.SessionOptions.ProtocolVersion = 3;
                connection.SessionOptions.SecureSocketLayer = useSsl;
                connection.Bind();

                return Ok(new { success = true, message = $"LDAP bind successful to '{request.Server}:{port}'." });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return Ok(new { success = false, error = "LDAP connection/bind failed." });
            }
        }
    }
}

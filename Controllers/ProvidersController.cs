using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpRouter.Core.Database;
using McpRouter.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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

        [HttpGet("secrets")]
        public async Task<IActionResult> GetSecretProviders()
        {
            try
            {
                var providers = await _secretRepo.GetSecretProvidersAsync();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("secrets")]
        public async Task<IActionResult> SaveSecretProvider([FromBody] SecretProviderDto dto, [FromServices] IAuditLogger auditLogger)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            var username = User?.Identity?.Name ?? "anonymous";
            try
            {
                McpRouter.Core.Security.SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                await _secretRepo.SaveSecretProviderAsync(dto);

                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, dto.ConfigJson ?? "", true);

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, dto.ConfigJson ?? "", false, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _ = auditLogger.LogAdminActionAsync(username, "SaveSecretProvider", dto.ProviderName, dto.ConfigJson ?? "", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuthProviders()
        {
            try
            {
                var providers = await _authRepo.GetAuthProvidersAsync();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("auth")]
        public async Task<IActionResult> SaveAuthProvider([FromBody] AuthProviderDto dto, [FromServices] IAuditLogger auditLogger)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            var username = User?.Identity?.Name ?? "anonymous";
            try
            {
                McpRouter.Core.Security.SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                await _authRepo.SaveAuthProviderAsync(dto);

                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, dto.ConfigJson ?? "", true);

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, dto.ConfigJson ?? "", false, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _ = auditLogger.LogAdminActionAsync(username, "SaveAuthProvider", dto.ProviderName, dto.ConfigJson ?? "", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

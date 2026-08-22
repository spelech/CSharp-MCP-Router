using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using McpRouter.Infrastructure.Secrets;
using McpRouter.Infrastructure.Identity;

namespace McpRouter.Components.AppKeys
{
    public class SaveCredentialRequest
    {
        public JsonElement SecretJson { get; set; }
    }

    [ApiController]
    [Route("api/user/credentials")]
    [Authorize]
    public class UserCredentialsController : ControllerBase
    {
        private readonly IUserSecretStore _userSecretStore;

        public UserCredentialsController(IUserSecretStore userSecretStore)
        {
            _userSecretStore = userSecretStore;
        }

        private async Task<UserIdentityContext> GetIdentityAsync()
        {
            var compositeProvider = HttpContext.RequestServices.GetRequiredService<CompositeIdentityProvider>();
            return await compositeProvider.ResolveIdentityAsync(HttpContext);
        }

        [HttpGet]
        public async Task<IActionResult> GetConfiguredCredentials()
        {
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;

            try
            {
                // We assume IUserSecretStore has GetServerIdsAsync or similar to list the configured credentials for a user.
                var serverIds = await _userSecretStore.GetServerIdsAsync(currentUser);
                return Ok(serverIds);
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost("{serverId}")]
        public async Task<IActionResult> SaveCredential(string serverId, [FromBody] SaveCredentialRequest request)
        {
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;

            if (string.IsNullOrWhiteSpace(serverId))
            {
                return BadRequest(new { error = "ServerId is required." });
            }

            try
            {
                string secretJsonString;
                
                // If they passed a string, use it. If they passed an object, serialize it.
                if (request.SecretJson.ValueKind == JsonValueKind.String)
                {
                    secretJsonString = request.SecretJson.GetString() ?? "";
                }
                else
                {
                    secretJsonString = request.SecretJson.GetRawText();
                }

                await _userSecretStore.SaveSecretAsync(currentUser, serverId, secretJsonString);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpDelete("{serverId}")]
        public async Task<IActionResult> DeleteCredential(string serverId)
        {
            var identity = await GetIdentityAsync();
            var currentUser = identity.Username;

            if (string.IsNullOrWhiteSpace(serverId))
            {
                return BadRequest(new { error = "ServerId is required." });
            }

            try
            {
                await _userSecretStore.DeleteSecretAsync(currentUser, serverId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger(GetType().Name).LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}

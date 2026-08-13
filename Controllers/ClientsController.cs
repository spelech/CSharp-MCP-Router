using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using McpRouter.Core.Database;
using McpRouter.Services;
using Microsoft.Extensions.DependencyInjection;
using Dapper;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class ClientsController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly McpRouter.Core.Logging.IAuditLogger _auditLogger;
        private readonly ICredentialService _credentialService;

        public ClientsController(IDbConnectionFactory dbFactory, McpRouter.Core.Logging.IAuditLogger _auditLogger, ICredentialService credentialService)
        {
            _dbFactory = dbFactory;
            this._auditLogger = _auditLogger;
            _credentialService = credentialService;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            using var conn = _dbFactory.CreateConnection();
            var keys = await conn.QueryAsync<dynamic>("SELECT Id, Name, Username, KeyPrefix, ScopesJson FROM AppKeys");

            var clients = keys.Select(k => {
                var scopesJson = Convert.ToString(k.ScopesJson) ?? "[]";
                List<string> scopes;
                try { scopes = JsonSerializer.Deserialize<List<string>>(scopesJson) ?? new List<string>(); }
                catch { scopes = new List<string>(); }

                return new {
                    Id = Convert.ToString(k.Id),
                    ClientId = Convert.ToString(k.Username) ?? Convert.ToString(k.KeyPrefix),
                    DisplayName = Convert.ToString(k.Name) ?? "App Key",
                    Scopes = scopes,
                    IsDynamic = false
                };
            }).ToList();

            return Ok(clients);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientModel model)
        {
            if (string.IsNullOrWhiteSpace(model.DisplayName))
                return BadRequest("DisplayName is required.");

            var clientId = Guid.NewGuid().ToString("N");
            var scopes = model.Scopes ?? new List<string>();

            var username = User?.Identity?.Name ?? "unknown";
            var ownerSid = "";

            try
            {
                var compositeProvider = HttpContext.RequestServices.GetService<McpRouter.Core.Identity.CompositeIdentityProvider>();
                if (compositeProvider != null)
                {
                    var identity = await compositeProvider.ResolveIdentityAsync(HttpContext);
                    if (identity != null)
                    {
                        username = identity.Username;
                        ownerSid = identity.AllSids.FirstOrDefault() ?? "";
                    }
                }
            }
            catch { }

            try
            {
                var (appKey, plaintextKey) = await _credentialService.CreateCredentialAsync(
                    model.DisplayName,
                    clientId, // Username/ClientId of the AppKey
                    ownerSid,
                    scopes,
                    null // Registered clients have no expiration by default
                );

                await _auditLogger.LogAdminActionAsync(username, "client.create", clientId, JsonSerializer.Serialize(new { model.DisplayName, Scopes = model.Scopes }), true);

                return Ok(new {
                    ClientId = clientId,
                    ClientSecret = plaintextKey,
                    DisplayName = model.DisplayName
                });
            }
            catch (Exception ex)
            {
                await _auditLogger.LogAdminActionAsync(username, "client.create", "", JsonSerializer.Serialize(new { model.DisplayName }), false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var username = User?.Identity?.Name ?? "unknown";
            try
            {
                try
                {
                    var compositeProvider = HttpContext.RequestServices.GetService<McpRouter.Core.Identity.CompositeIdentityProvider>();
                    if (compositeProvider != null)
                    {
                        var identity = await compositeProvider.ResolveIdentityAsync(HttpContext);
                        if (identity != null)
                        {
                            username = identity.Username;
                        }
                    }
                }
                catch { }

                var success = await _credentialService.RevokeCredentialAsync(id);
                if (!success)
                {
                    await _auditLogger.LogAdminActionAsync(username, "client.delete", id, "", false, "Client not found");
                    return NotFound();
                }

                await _auditLogger.LogAdminActionAsync(username, "client.delete", id, "", true);
                return NoContent();
            }
            catch (Exception ex)
            {
                await _auditLogger.LogAdminActionAsync(username, "client.delete", id, "", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class CreateClientModel
        {
            public string DisplayName { get; set; } = string.Empty;
            public List<string> Scopes { get; set; } = new();
        }
    }
}

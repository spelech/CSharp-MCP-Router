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
            var keys = await conn.QueryAsync<dynamic>("SELECT Id, Name, Username, KeyPrefix, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys");

            var clients = keys.Select(k =>
            {
                var scopesJson = Convert.ToString(k.ScopesJson) ?? "[]";
                List<string> scopes;
                try { scopes = JsonSerializer.Deserialize<List<string>>(scopesJson) ?? new List<string>(); }
                catch { scopes = new List<string>(); }

                return new
                {
                    Id = Convert.ToString(k.Id),
                    ClientId = Convert.ToString(k.Username) ?? Convert.ToString(k.KeyPrefix),
                    DisplayName = Convert.ToString(k.Name) ?? "App Key",
                    Scopes = scopes,
                    ExpiresAt = k.ExpiresAt != null ? (DateTime?)Convert.ToDateTime(k.ExpiresAt) : null,
                    CreatedAt = k.CreatedAt != null ? (DateTime?)Convert.ToDateTime(k.CreatedAt) : null,
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

            try
            {
                // CRITICAL SECURITY: Decouple creator SID from client credentials.
                // Client credentials must NOT inherit administrative privileges or creator's SID.
                var (appKey, plaintextKey) = await _credentialService.CreateCredentialAsync(
                    model.DisplayName,
                    clientId, // Username/ClientId of the AppKey
                    string.Empty, // No administrative SID assigned to machine/client credentials
                    scopes,
                    model.ExpiresInDays
                );

                await _auditLogger.LogAdminActionAsync(username, "client.create", clientId, JsonSerializer.Serialize(new { model.DisplayName, Scopes = model.Scopes, model.ExpiresInDays }), true);

                return Ok(new
                {
                    ClientId = clientId,
                    ClientSecret = plaintextKey,
                    DisplayName = model.DisplayName,
                    ExpiresAt = appKey.ExpiresAt
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
            public int? ExpiresInDays { get; set; }
        }
    }
}

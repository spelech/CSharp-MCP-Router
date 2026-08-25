using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpRouter.Components.Clients
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class ClientsController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IAuditLogger _auditLogger;
        private readonly ICredentialService _credentialService;

        public ClientsController(IDbConnectionFactory dbFactory, IAuditLogger auditLogger, ICredentialService credentialService)
        {
            _dbFactory = dbFactory;
            _auditLogger = auditLogger;
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
            {
                return BadRequest("DisplayName is required.");
            }

            var clientId = Guid.NewGuid().ToString("N");
            var scopes = model.Scopes ?? new List<string>();

            var username = User?.Identity?.Name ?? "unknown";
            UserIdentityContext? identity = null;

            var httpCtx = HttpContext;
            if (httpCtx?.RequestServices != null)
            {
                try
                {
                    var compositeProvider = httpCtx.RequestServices.GetService<CompositeIdentityProvider>();
                    if (compositeProvider != null)
                    {
                        identity = await compositeProvider.ResolveIdentityAsync(httpCtx);
                        if (identity != null)
                        {
                            username = identity.Username;
                        }
                    }
                }
                catch { }
            }

            var config = httpCtx?.RequestServices?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var isAdmin = (identity != null && SecurityValidationHelper.IsAdmin(identity, config))
                || User?.IsInRole("Admin") == true
                || User?.Claims.Any(c => c.Value == "full_admin" || c.Value == "S-1-5-32-544") == true
                || httpCtx == null;

            // Validate requested category scopes
            foreach (var scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    continue;
                }

                var trimmed = scope.Trim();
                if (trimmed.StartsWith("category:", StringComparison.OrdinalIgnoreCase))
                {
                    var catName = trimmed.Substring("category:".Length).Trim();
                    if (string.IsNullOrWhiteSpace(catName))
                    {
                        return BadRequest("Category scope cannot be empty.");
                    }

                    if (!isAdmin)
                    {
                        var registeredCategories = await GetRegisteredCategoriesAsync();
                        if (!registeredCategories.Any(c => string.Equals(c, catName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return BadRequest($"Category '{catName}' does not exist among registered servers.");
                        }
                    }
                }
            }

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
                HttpContext?.RequestServices?.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(GetType().Name)?.LogError(ex, "An unexpected error occurred.");
                await _auditLogger.LogAdminActionAsync(username, "client.create", "", JsonSerializer.Serialize(new { model.DisplayName }), false, ex.Message);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        private async Task<List<string>> GetRegisteredCategoriesAsync()
        {
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = _dbFactory.CreateConnection();
                var rawList = await conn.QueryAsync<string>("SELECT Categories FROM Servers WHERE Enabled = 1");
                foreach (var rawCat in rawList)
                {
                    if (string.IsNullOrWhiteSpace(rawCat))
                    {
                        continue;
                    }

                    try
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(rawCat);
                        if (list != null)
                        {
                            foreach (var c in list)
                            {
                                if (!string.IsNullOrWhiteSpace(c))
                                {
                                    categories.Add(c.Trim());
                                }
                            }
                        }
                    }
                    catch
                    {
                        var parts = rawCat.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var p in parts)
                        {
                            categories.Add(p);
                        }
                    }
                }
            }
            catch
            {
                // If Servers table does not exist or DB error
            }
            return categories.ToList();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var username = User?.Identity?.Name ?? "unknown";
            try
            {
                try
                {
                    var compositeProvider = HttpContext.RequestServices.GetService<CompositeIdentityProvider>();
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
                HttpContext?.RequestServices?.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger(GetType().Name)?.LogError(ex, "An unexpected error occurred.");
                await _auditLogger.LogAdminActionAsync(username, "client.delete", id, "", false, ex.Message);
                return StatusCode(500, new { error = "An unexpected error occurred." });
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



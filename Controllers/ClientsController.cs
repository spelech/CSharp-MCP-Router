using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using McpRouter.Core.Database;
using Dapper;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class ClientsController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;

        public ClientsController(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
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
            var clientSecret = "mcp_" + Guid.NewGuid().ToString("N");
            var keyId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var prefix = clientSecret.Substring(0, Math.Min(16, clientSecret.Length));
            var scopesJson = JsonSerializer.Serialize(model.Scopes ?? new List<string>());

            using var conn = _dbFactory.CreateConnection();
            await conn.ExecuteAsync(@"
                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson)
                VALUES (@Id, @Name, @Username, @KeyPrefix, @EncryptedKey, @ScopesJson)",
                new {
                    Id = keyId,
                    Name = model.DisplayName,
                    Username = clientId,
                    KeyPrefix = prefix,
                    EncryptedKey = clientSecret,
                    ScopesJson = scopesJson
                });

            return Ok(new {
                ClientId = clientId,
                ClientSecret = clientSecret,
                DisplayName = model.DisplayName
            });
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            using var conn = _dbFactory.CreateConnection();
            var deleted = await conn.ExecuteAsync("DELETE FROM AppKeys WHERE Id = @id", new { id });
            if (deleted == 0) return NotFound();
            
            return NoContent();
        }

        public class CreateClientModel
        {
            public string DisplayName { get; set; } = string.Empty;
            public List<string> Scopes { get; set; } = new();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Microsoft.EntityFrameworkCore;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Assuming we'd want this protected by TinyAuth in a real deployment
    public class ClientsController : ControllerBase
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public ClientsController(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var clients = new List<object>();
            await foreach (var app in _applicationManager.ListAsync(null, null, HttpContext.RequestAborted))
            {
                var clientId = await _applicationManager.GetClientIdAsync(app);
                var displayName = await _applicationManager.GetDisplayNameAsync(app);
                var perms = await _applicationManager.GetPermissionsAsync(app);
                
                // Roles/Servers are stored as permissions prefixed with "scp:" (scopes) or "role:"
                var scopes = perms.Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope)).Select(p => p.Substring(4));
                var isDynamic = perms.Contains("scp:dynamic_client");
                
                clients.Add(new {
                    Id = await _applicationManager.GetIdAsync(app),
                    ClientId = clientId,
                    DisplayName = displayName ?? "Unknown",
                    Scopes = scopes,
                    IsDynamic = isDynamic
                });
            }
            return Ok(clients);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientModel model)
        {
            if (string.IsNullOrWhiteSpace(model.DisplayName))
                return BadRequest("DisplayName is required.");

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = Guid.NewGuid().ToString("N"),
                ClientSecret = Guid.NewGuid().ToString("N"), // Auto-generate secret
                DisplayName = model.DisplayName,
                Permissions = 
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "mcp_client"
                }
            };

            if (model.Scopes != null)
            {
                foreach(var scope in model.Scopes)
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
                }
            }

            var app = await _applicationManager.CreateAsync(descriptor);
            
            return Ok(new {
                ClientId = descriptor.ClientId,
                ClientSecret = descriptor.ClientSecret, // Return once so the user can copy it
                DisplayName = descriptor.DisplayName
            });
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var app = await _applicationManager.FindByIdAsync(id);
            if (app == null) return NotFound();
            
            await _applicationManager.DeleteAsync(app);
            return NoContent();
        }

        public class CreateClientModel
        {
            public string DisplayName { get; set; } = string.Empty;
            public List<string> Scopes { get; set; } = new();
        }
    }
}

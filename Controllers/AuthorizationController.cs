using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Text.Json;

namespace McpRouter.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        [HttpPost("~/connect/token")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
                if (application == null)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application was not found in the directory."
                        }));
                }

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // Subject (sub) is a required claim
                identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId!);
                identity.AddClaim(OpenIddictConstants.Claims.Name, await _applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!);

                identity.SetDestinations(static claim => new[] { OpenIddictConstants.Destinations.AccessToken });

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new NotImplementedException("The specified grant type is not implemented.");
        }

        [HttpPost("~/api/register")]
        [Produces("application/json")]
        public async Task<IActionResult> RegisterClient([FromBody] JsonElement metadata)
        {
            var clientName = metadata.TryGetProperty("client_name", out var cn) ? cn.GetString() : "Unknown Client";
            var clientId = Guid.NewGuid().ToString("N");
            var clientSecret = Guid.NewGuid().ToString("N");

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                DisplayName = clientName,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            };

            await _applicationManager.CreateAsync(descriptor);

            return Ok(new
            {
                client_id = clientId,
                client_secret = clientSecret,
                client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                client_secret_expires_at = 0,
                token_endpoint_auth_method = "client_secret_post"
            });
        }
    }
}

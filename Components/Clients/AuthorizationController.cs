using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Text.Json;

using McpRouter.Infrastructure.Logging;

namespace McpRouter.Components.Clients
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IAuditLogger _auditLogger;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager, IAuditLogger auditLogger)
        {
            _applicationManager = applicationManager;
            _auditLogger = auditLogger;
        }

        [HttpPost("~/connect/token")]
        [HttpPost("~/oauth/token")]
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

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded || result.Principal == null)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }));
                }

                var identity = new ClaimsIdentity(result.Principal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                identity.SetDestinations(static claim => new[] { OpenIddictConstants.Destinations.AccessToken });
                
                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new NotImplementedException("The specified grant type is not implemented.");
        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Attempt to authenticate the user using either OIDC Headers or AppKey
            var result = await HttpContext.AuthenticateAsync("OidcHeader");
            if (!result.Succeeded || result.Principal == null)
            {
                result = await HttpContext.AuthenticateAsync("AppKey");
            }

            if (!result.Succeeded || result.Principal == null)
            {
                return Content("<html><body><div style='font-family:sans-serif; text-align:center; padding-top: 50px;'><h1>Access Denied</h1><p>Please authenticate through your SSO portal or provide valid proxy headers to access this consent screen.</p></div></body></html>", "text/html");
            }

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
            if (application == null)
            {
                return BadRequest(new { error = "invalid_client", error_description = "The client application was not found." });
            }
            
            var clientName = await _applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!;
            var username = result.Principal.Identity?.Name ?? "Unknown";

            // Handle Form Post (Accept/Deny)
            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                var form = await HttpContext.Request.ReadFormAsync();
                if (form.ContainsKey("submit.Deny"))
                {
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Accept
                var identity = new ClaimsIdentity(result.Principal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                identity.AddClaim(OpenIddictConstants.Claims.Subject, username);
                identity.SetDestinations(static claim => new[] { OpenIddictConstants.Destinations.AccessToken });

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Redirect to React App (GET)
            var qs = HttpContext.Request.QueryString.HasValue ? HttpContext.Request.QueryString.Value : "";
            if (!qs.Contains("client_name=")) 
            {
                var encodedName = Uri.EscapeDataString(clientName);
                qs = string.IsNullOrEmpty(qs) ? $"?client_name={encodedName}" : $"{qs}&client_name={encodedName}";
            }
            return Redirect($"/consent{qs}");
        }


        [HttpPost("~/api/register")]
        [Produces("application/json")]
        public async Task<IActionResult> RegisterClient([FromBody] JsonElement metadata)
        {
            var embeddingService = HttpContext.RequestServices.GetRequiredService<McpRouter.Core.Routing.DynamicEmbeddingService>();
            var settings = embeddingService.GetSettings();
            
            if (!settings.AllowOpenClientRegistration)
            {
                var authService = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
                var authResult = await authService.AuthorizeAsync(User, "AdminPolicy");
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }
            }

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
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            };

            try
            {
                await _applicationManager.CreateAsync(descriptor);

                var username = User?.Identity?.Name ?? "unknown";
                await _auditLogger.LogAdminActionAsync(username, "oauth.client.register", clientId, JsonSerializer.Serialize(new { clientName }), true);

                return Ok(new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    client_secret_expires_at = 0,
                    token_endpoint_auth_method = "client_secret_post"
                });
            }
            catch (Exception ex)
            {
                var username = User?.Identity?.Name ?? "unknown";
                await _auditLogger.LogAdminActionAsync(username, "oauth.client.register", clientId, JsonSerializer.Serialize(new { clientName }), false, ex.Message);
                throw;
            }
        }
    }
}



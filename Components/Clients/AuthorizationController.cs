using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace ModelContextGateway.Components.Clients
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager? _applicationManager;
        private readonly IAuditLogger _auditLogger;
        private readonly IDbConnectionFactory? _dbFactory;
        private readonly ICredentialService? _credentialService;

        [ActivatorUtilitiesConstructor]
        public AuthorizationController(
            IDbConnectionFactory dbFactory,
            ICredentialService credentialService,
            IAuditLogger auditLogger,
            IOpenIddictApplicationManager? applicationManager = null)
        {
            _dbFactory = dbFactory;
            _credentialService = credentialService;
            _auditLogger = auditLogger;
            _applicationManager = applicationManager;
        }

        public AuthorizationController(IOpenIddictApplicationManager applicationManager, IAuditLogger auditLogger)
            : this(null!, null!, auditLogger, applicationManager)
        {
        }

        private async Task<object?> FindApplicationAsync(string clientId)
        {
            if (_applicationManager != null)
            {
                return await _applicationManager.FindByClientIdAsync(clientId);
            }
            if (_dbFactory != null)
            {
                using var conn = _dbFactory.CreateConnection();
                return await conn.QuerySingleOrDefaultAsync<AppKey>(
                    "SELECT * FROM AppKeys WHERE Username = @Id OR KeyPrefix = @Id",
                    new { Id = clientId });
            }
            return null;
        }

        private async Task<string?> GetDisplayNameAsync(object application)
        {
            if (_applicationManager != null)
            {
                return await _applicationManager.GetDisplayNameAsync(application);
            }
            if (application is AppKey appKey)
            {
                return appKey.Name;
            }
            return null;
        }

        private async Task<object?> CreateApplicationAsync(OpenIddictApplicationDescriptor descriptor)
        {
            if (_applicationManager != null)
            {
                return await _applicationManager.CreateAsync(descriptor);
            }
            if (_credentialService != null)
            {
                var scopes = descriptor.Permissions
                    .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
                    .Select(p => p.Substring(OpenIddictConstants.Permissions.Prefixes.Scope.Length))
                    .ToList();

                if (scopes.Count == 0)
                {
                    scopes.Add("all");
                }

                var (appKey, plaintext) = await _credentialService.CreateCredentialAsync(
                    descriptor.DisplayName ?? "Dynamic Client",
                    descriptor.ClientId ?? Guid.NewGuid().ToString("N"),
                    string.Empty,
                    scopes,
                    null
                );
                return appKey;
            }
            return null;
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
                var application = await FindApplicationAsync(request.ClientId!);
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
                identity.AddClaim(OpenIddictConstants.Claims.Name, await GetDisplayNameAsync(application) ?? request.ClientId!);

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
        [HttpGet("~/oauth/authorize")]
        [HttpPost("~/oauth/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Attempt to authenticate the user using either OIDC Headers, AppKey, or standard auth
            var result = await HttpContext.AuthenticateAsync("OidcHeader");
            if (!result.Succeeded || result.Principal == null)
            {
                result = await HttpContext.AuthenticateAsync("AppKey");
            }
            if (!result.Succeeded || result.Principal == null)
            {
                result = await HttpContext.AuthenticateAsync();
            }

            var principal = (result.Succeeded && result.Principal != null) ? result.Principal : User;
            var config = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

            if (principal?.Identity?.IsAuthenticated != true)
            {
                if (config != null && SecurityValidationHelper.IsStandaloneAdminNetwork(HttpContext.Connection.RemoteIpAddress, config))
                {
                    if (!SecurityValidationHelper.HasExternalIdp(config, HttpContext))
                    {
                        var standaloneIdentity = new ClaimsIdentity("Standalone");
                        standaloneIdentity.AddClaim(new Claim(ClaimTypes.Name, "admin"));
                        standaloneIdentity.AddClaim(new Claim(ClaimTypes.Role, "Administrator"));
                        principal = new ClaimsPrincipal(standaloneIdentity);
                    }
                }
            }

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return Content("<html><body><div style='font-family:sans-serif; text-align:center; padding-top: 50px;'><h1>Access Denied</h1><p>Please authenticate through your SSO portal or provide valid proxy headers to access this consent screen.</p></div></body></html>", "text/html");
            }

            var application = await FindApplicationAsync(request.ClientId!);
            if (application == null)
            {
                return BadRequest(new { error = "invalid_client", error_description = "The client application was not found." });
            }

            var clientName = await GetDisplayNameAsync(application) ?? request.ClientId!;
            var username = principal.Identity?.Name ?? "Unknown";

            // Handle Form Post (Accept/Deny)
            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                var form = await HttpContext.Request.ReadFormAsync();
                if (form.ContainsKey("submit.Deny"))
                {
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Accept
                var claims = principal.Claims.ToList();
                if (!claims.Any(c => c.Type == OpenIddictConstants.Claims.Subject))
                {
                    claims.Add(new Claim(OpenIddictConstants.Claims.Subject, username));
                }
                var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
        [HttpPost("~/connect/register")]
        [HttpPost("~/oauth/register")]
        [HttpPost("~/register")]
        [Produces("application/json")]
        public async Task<IActionResult> RegisterClient([FromBody] JsonElement metadata)
        {
            var embeddingService = HttpContext.RequestServices.GetRequiredService<ModelContextGateway.Core.Routing.DynamicEmbeddingService>();
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
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "mcp_client",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "openid",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"
                }
            };

            var redirectUrisList = new List<string>();
            if (metadata.TryGetProperty("redirect_uris", out var rUris) && rUris.ValueKind == JsonValueKind.Array)
            {
                foreach (var uri in rUris.EnumerateArray())
                {
                    var uriStr = uri.GetString();
                    if (!string.IsNullOrEmpty(uriStr))
                    {
                        redirectUrisList.Add(uriStr);
                        if (Uri.TryCreate(uriStr, UriKind.Absolute, out var parsedUri))
                        {
                            descriptor.RedirectUris.Add(parsedUri);
                        }
                    }
                }
            }

            var grantTypesList = new List<string> { "authorization_code", "refresh_token" };
            if (metadata.TryGetProperty("grant_types", out var gTypes) && gTypes.ValueKind == JsonValueKind.Array)
            {
                grantTypesList.Clear();
                foreach (var gt in gTypes.EnumerateArray())
                {
                    var gtStr = gt.GetString();
                    if (!string.IsNullOrEmpty(gtStr))
                    {
                        grantTypesList.Add(gtStr);
                    }
                }
            }

            var responseTypesList = new List<string> { "code" };
            if (metadata.TryGetProperty("response_types", out var respTypes) && respTypes.ValueKind == JsonValueKind.Array)
            {
                responseTypesList.Clear();
                foreach (var rt in respTypes.EnumerateArray())
                {
                    var rtStr = rt.GetString();
                    if (!string.IsNullOrEmpty(rtStr))
                    {
                        responseTypesList.Add(rtStr);
                    }
                }
            }

            var authMethod = metadata.TryGetProperty("token_endpoint_auth_method", out var team) ? team.GetString() : "client_secret_post";

            try
            {
                await CreateApplicationAsync(descriptor);

                var username = User?.Identity?.Name ?? "unknown";
                await _auditLogger.LogAdminActionAsync(username, "oauth.client.register", clientId, JsonSerializer.Serialize(new { clientName, redirectUris = redirectUrisList }), true);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    client_name = clientName,
                    client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    client_secret_expires_at = 0,
                    redirect_uris = redirectUrisList,
                    grant_types = grantTypesList,
                    response_types = responseTypesList,
                    token_endpoint_auth_method = authMethod ?? "client_secret_post"
                });
            }
            catch (Exception ex)
            {
                var username = User?.Identity?.Name ?? "unknown";
                await _auditLogger.LogAdminActionAsync(username, "oauth.client.register", clientId, JsonSerializer.Serialize(new { clientName, redirectUris = redirectUrisList }), false, ex.Message);
                throw;
            }
        }
    }
}



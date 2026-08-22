using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using McpRouter.Models;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Secrets;
using Dapper;

namespace McpRouter.Middleware
{
    public class AppKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;

        public AppKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IDbConnectionFactory dbFactory,
            IConfiguration config)
            : base(options, logger, encoder)
        {
            _dbFactory = dbFactory;
            _config = config;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? token = null;
            string authHeader = Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                var xAppKey = Request.Headers["X-App-Key"].ToString();
                if (!string.IsNullOrEmpty(xAppKey))
                {
                    token = xAppKey.Trim();
                }
                else
                {
                    var xApiKey = Request.Headers["X-Api-Key"].ToString();
                    if (!string.IsNullOrEmpty(xApiKey))
                    {
                        token = xApiKey.Trim();
                    }
                    else if (Request.Query.TryGetValue("app_key", out var qAppKey) && !string.IsNullOrEmpty(qAppKey))
                    {
                        token = qAppKey.ToString().Trim();
                    }
                    else if (Request.Query.TryGetValue("api_key", out var qApiKey) && !string.IsNullOrEmpty(qApiKey))
                    {
                        token = qApiKey.ToString().Trim();
                    }
                    else if (Request.Query.TryGetValue("key", out var qKey) && !string.IsNullOrEmpty(qKey))
                    {
                        token = qKey.ToString().Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(token) || !token.StartsWith("mcp-"))
            {
                return AuthenticateResult.NoResult();
            }

            try
            {
                // Extract selector prefix: for high-entropy tokens formatted as mcp-{scopeSlug}-{selector}-{secret},
                // prefix is mcp-{scopeSlug}-{selector}. Fallback safely for backward-compatible/test formats.
                string prefix;
                var parts = token.Split('-');
                if (parts.Length >= 4 && parts[0] == "mcp")
                {
                    prefix = $"{parts[0]}-{parts[1]}-{parts[2]}";
                }
                else if (parts.Length == 3 && parts[0] == "mcp" && parts[2].Length >= 32)
                {
                    prefix = $"{parts[0]}-{parts[1]}-{parts[2].Substring(0, 32)}";
                }
                else
                {
                    prefix = token.Length > 16 ? token.Substring(0, 16) : token;
                }

                using var conn = _dbFactory.CreateConnection();
                AppKey? appKey = null;

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = "SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix;";
                    appKey = await conn.QueryFirstOrDefaultAsync<AppKey>(sql, new { KeyPrefix = prefix });
                }
                else
                {
                    const string sql = "SELECT Id, Name, Username, OwnerSid, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys WHERE KeyPrefix = @KeyPrefix;";
                    appKey = await conn.QueryFirstOrDefaultAsync<AppKey>(sql, new { KeyPrefix = prefix });
                }

                if (appKey == null)
                {
                    return AuthenticateResult.Fail("Invalid App Key prefix.");
                }

                // Verify the key using constant-time comparison on SHA-256 hash
                if (appKey.EncryptedKey.Length != 64 || appKey.EncryptedKey.Contains("/") || appKey.EncryptedKey.Contains("+") || appKey.EncryptedKey.Contains("="))
                {
                    return AuthenticateResult.Fail("Invalid App Key hash format.");
                }

                using var sha = SHA256.Create();
                var computedBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
                var computedHash = Convert.ToHexString(computedBytes).ToLowerInvariant();

                bool isValid = CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(appKey.EncryptedKey.ToLowerInvariant()),
                    System.Text.Encoding.UTF8.GetBytes(computedHash)
                );

                if (!isValid)
                {
                    return AuthenticateResult.Fail("Invalid App Key.");
                }

                // Check expiration
                if (appKey.ExpiresAt.HasValue && appKey.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return AuthenticateResult.Fail("App Key has expired.");
                }

                // Successfully authenticated!
                var claims = new System.Collections.Generic.List<Claim>
                {
                    new Claim(ClaimTypes.Name, appKey.Username),
                    new Claim(ClaimTypes.Role, "McpClient")
                };

                bool isAdminAppKey = false;
                bool isSystemKey = string.Equals(appKey.KeyType, "system", StringComparison.OrdinalIgnoreCase);

                if (isSystemKey && !string.IsNullOrWhiteSpace(appKey.ScopesJson))
                {
                    try
                    {
                        var parsedScopes = JsonSerializer.Deserialize<List<string>>(appKey.ScopesJson);
                        if (parsedScopes != null && parsedScopes.Any(s =>
                            string.Equals(s, "admin", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "*", StringComparison.OrdinalIgnoreCase)))
                        {
                            isAdminAppKey = true;
                        }
                    }
                    catch
                    {
                        var scopeParts = appKey.ScopesJson.Split(new[] { ',', '[', ']', '"', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (scopeParts.Any(s =>
                            string.Equals(s, "admin", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "*", StringComparison.OrdinalIgnoreCase)))
                        {
                            isAdminAppKey = true;
                        }
                    }
                }

                if (isAdminAppKey)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                    claims.Add(new Claim("Scope", "admin"));
                }

                if (!string.IsNullOrEmpty(appKey.OwnerSid))
                {
                    claims.Add(new Claim("Sid", appKey.OwnerSid));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                // Store AppKey details in HttpContext for authorization checks
                Context.Items["AppKeyUsed"] = true;
                Context.Items["AppKeyScopes"] = appKey.ScopesJson;
                Context.Items["AppKeyOwner"] = appKey.Username;
                Context.Items["AppKeyOwnerSid"] = appKey.OwnerSid;
                Context.Items["AppKeyType"] = appKey.KeyType;

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating App Key.");
                return AuthenticateResult.Fail("Error validating App Key.");
            }
        }
    }
}

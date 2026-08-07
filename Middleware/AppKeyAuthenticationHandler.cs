using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using McpRouter.Models;
using McpRouter.Core.Secrets;
using Dapper;
using McpRouter.Core.Database;

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

            if (string.IsNullOrEmpty(token) || !token.StartsWith("mcp-"))
            {
                return AuthenticateResult.NoResult();
            }

            try
            {
                // Prefix is first 16 characters
                var prefix = token.Length > 16 ? token.Substring(0, 16) : token;

                using var conn = _dbFactory.CreateConnection();
                AppKey? appKey = null;

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = "SELECT * FROM AppKeys WHERE KeyPrefix = @KeyPrefix;";
                    appKey = await conn.QueryFirstOrDefaultAsync<AppKey>(sql, new { KeyPrefix = prefix });
                }
                else
                {
                    const string sql = "SELECT Id, Name, Username, KeyPrefix, EncryptedKey, ScopesJson, ExpiresAt, CreatedAt FROM AppKeys WHERE KeyPrefix = @KeyPrefix;";
                    appKey = await conn.QueryFirstOrDefaultAsync<AppKey>(sql, new { KeyPrefix = prefix });
                }

                if (appKey == null)
                {
                    return AuthenticateResult.Fail("Invalid App Key prefix.");
                }

                // Verify the key using constant-time comparison
                bool isValid = false;
                if (appKey.EncryptedKey.Length == 64 && !appKey.EncryptedKey.Contains("/") && !appKey.EncryptedKey.Contains("+") && !appKey.EncryptedKey.Contains("="))
                {
                    // SHA-256 Hash
                    using var sha = SHA256.Create();
                    var computedBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
                    var computedHash = Convert.ToHexString(computedBytes).ToLowerInvariant();
                    
                    isValid = CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(appKey.EncryptedKey),
                        System.Text.Encoding.UTF8.GetBytes(computedHash)
                    );
                }
                else
                {
                    // Legacy AES Decryption (fallback)
                    try
                    {
                        var decrypted = SymmetricEncryptionHelper.Decrypt(appKey.EncryptedKey, _config);
                        
                        isValid = CryptographicOperations.FixedTimeEquals(
                            System.Text.Encoding.UTF8.GetBytes(decrypted),
                            System.Text.Encoding.UTF8.GetBytes(token)
                        );
                    }
                    catch
                    {
                        isValid = false;
                    }
                }

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
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, appKey.Username),
                    new Claim(ClaimTypes.Role, "McpClient")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                // Store AppKey details in HttpContext for authorization checks
                Context.Items["AppKeyUsed"] = true;
                Context.Items["AppKeyScopes"] = appKey.ScopesJson;
                Context.Items["AppKeyOwner"] = appKey.Username;

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

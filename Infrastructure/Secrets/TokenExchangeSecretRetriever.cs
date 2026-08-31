using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace ModelContextGateway.Infrastructure.Secrets
{
    public class TokenExchangeSecretRetriever : ISecretRetriever
    {
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly IMemoryCache? _cache;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly ISecretProviderRepository? _secretRepo;
        private readonly IAuthProviderRepository? _authRepo;
        private readonly IConfiguration? _config;
        private readonly ILogger<TokenExchangeSecretRetriever>? _logger;

        public string ProviderName => "TokenExchange";

        public TokenExchangeSecretRetriever(
            IHttpClientFactory? httpClientFactory = null,
            IMemoryCache? cache = null,
            IHttpContextAccessor? httpContextAccessor = null,
            ISecretProviderRepository? secretRepo = null,
            IAuthProviderRepository? authRepo = null,
            IConfiguration? config = null,
            ILogger<TokenExchangeSecretRetriever>? logger = null)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _secretRepo = secretRepo;
            _authRepo = authRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<string?> GetSecretAsync(string secretPath, string keyName)
        {
            var options = await ResolveTokenExchangeOptionsAsync(secretPath, keyName);

            if (string.IsNullOrWhiteSpace(options.TokenEndpoint))
            {
                throw new InvalidOperationException($"TokenExchange failed for path '{secretPath}': TokenEndpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                throw new InvalidOperationException($"TokenExchange failed for path '{secretPath}': ClientId is not configured.");
            }

            var subjectInfo = ResolveSubjectContext();
            var subjectToken = subjectInfo.SubjectToken;
            var subject = subjectInfo.SubjectName;

            string scope = !string.IsNullOrWhiteSpace(keyName) ? keyName : (options.Scope ?? string.Empty);
            string subjectKey = !string.IsNullOrWhiteSpace(subjectToken) ? subjectToken : (subject ?? "anonymous");
            string cacheKey = $"token_exchange:{options.ClientId}:{subjectKey}:{scope}:{options.TokenEndpoint}";

            if (_cache != null && _cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                _logger?.LogDebug("TokenExchange cache hit for subject '{Subject}' and scope '{Scope}'", subject, scope);
                return cachedToken;
            }

            var client = _httpClientFactory?.CreateClient("McpClient") ?? new HttpClient();

            var formFields = new Dictionary<string, string>
            {
                ["grant_type"] = !string.IsNullOrWhiteSpace(options.GrantType) ? options.GrantType : "urn:ietf:params:oauth:grant-type:token-exchange",
                ["client_id"] = options.ClientId,
            };

            if (!string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                formFields["client_secret"] = options.ClientSecret;
            }

            string grantType = formFields["grant_type"].ToLowerInvariant();

            if (grantType == "urn:ietf:params:oauth:grant-type:jwt-bearer")
            {
                if (!string.IsNullOrWhiteSpace(subjectToken))
                {
                    formFields["assertion"] = subjectToken;
                }
                else if (!string.IsNullOrWhiteSpace(subject))
                {
                    formFields["assertion"] = subject;
                }
                formFields["requested_token_use"] = "on_behalf_of";
            }
            else if (grantType == "urn:ietf:params:oauth:grant-type:token-exchange")
            {
                if (!string.IsNullOrWhiteSpace(subjectToken))
                {
                    formFields["subject_token"] = subjectToken;
                    formFields["subject_token_type"] = !string.IsNullOrWhiteSpace(options.SubjectTokenType)
                        ? options.SubjectTokenType
                        : "urn:ietf:params:oauth:token-type:access_token";
                }
                else if (!string.IsNullOrWhiteSpace(subject))
                {
                    formFields["subject_token"] = subject;
                    formFields["subject_token_type"] = !string.IsNullOrWhiteSpace(options.SubjectTokenType)
                        ? options.SubjectTokenType
                        : "urn:ietf:params:oauth:token-type:access_token";
                }

                if (!string.IsNullOrWhiteSpace(options.RequestedTokenType))
                {
                    formFields["requested_token_type"] = options.RequestedTokenType;
                }
            }

            if (!string.IsNullOrWhiteSpace(scope))
            {
                formFields["scope"] = scope;
            }
            else if (!string.IsNullOrWhiteSpace(options.Scope))
            {
                formFields["scope"] = options.Scope;
            }

            if (!string.IsNullOrWhiteSpace(options.Audience))
            {
                formFields["audience"] = options.Audience;
            }

            _logger?.LogInformation("Executing OAuth2 Token Exchange for subject '{Subject}' at endpoint '{Endpoint}'", subject, options.TokenEndpoint);

            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(formFields)
            };

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("TokenExchange request failed with status {StatusCode}. Body: {Body}", response.StatusCode, responseBody);
                throw new System.Security.SecurityException($"TokenExchange to '{options.TokenEndpoint}' failed with status {response.StatusCode}: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            string? accessToken = null;
            if (root.TryGetProperty("access_token", out var tokenProp))
            {
                accessToken = tokenProp.GetString();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new System.Security.SecurityException("TokenExchange response did not contain a valid 'access_token'.");
            }

            int expiresIn = 3600;
            if (root.TryGetProperty("expires_in", out var expProp))
            {
                if (expProp.ValueKind == JsonValueKind.Number)
                {
                    expiresIn = expProp.GetInt32();
                }
                else if (expProp.ValueKind == JsonValueKind.String && int.TryParse(expProp.GetString(), out var parsedExp))
                {
                    expiresIn = parsedExp;
                }
            }

            if (_cache != null && expiresIn > 0)
            {
                var cacheTtl = TimeSpan.FromSeconds(Math.Max(30, expiresIn - 30));
                _cache.Set(cacheKey, accessToken, cacheTtl);
            }

            return accessToken;
        }

        private (string? SubjectToken, string? SubjectName) ResolveSubjectContext()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                return (null, "system");
            }

            string? subjectToken = null;
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authVals))
            {
                var rawAuth = authVals.ToString();
                if (rawAuth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = rawAuth.Substring(7).Trim();
                    if (!token.StartsWith("mcp-", StringComparison.OrdinalIgnoreCase))
                    {
                        subjectToken = token;
                    }
                }
            }

            if (string.IsNullOrEmpty(subjectToken) && httpContext.Request.Headers.TryGetValue("X-Subject-Token", out var subjVals))
            {
                subjectToken = subjVals.ToString().Trim();
            }

            if (string.IsNullOrEmpty(subjectToken) && httpContext.Request.Headers.TryGetValue("X-Target-Auth", out var targetAuthVals))
            {
                subjectToken = targetAuthVals.ToString().Trim();
            }

            string? username = null;
            if (httpContext.Items.TryGetValue("UserIdentityContext", out var ctxObj) && ctxObj is UserIdentityContext userCtx)
            {
                username = userCtx.Username;
            }
            else if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                username = httpContext.User.Identity.Name;
            }

            return (subjectToken, username ?? "anonymous");
        }

        private async Task<TokenExchangeOptions> ResolveTokenExchangeOptionsAsync(string secretPath, string keyName)
        {
            var options = new TokenExchangeOptions();

            if (_secretRepo != null)
            {
                try
                {
                    var secretProviders = await _secretRepo.GetSecretProvidersAsync();
                    var teProvider = secretProviders?.FindProvider("TokenExchange")
                        ?? secretProviders?.FindProvider("PocketID")
                        ?? secretProviders?.FindProvider("OIDC");

                    if (teProvider != null && teProvider.IsEnabled && !string.IsNullOrWhiteSpace(teProvider.ConfigJson))
                    {
                        options.PopulateFromJson(teProvider.ConfigJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load TokenExchange configuration from SecretProviders repository");
                }
            }

            if (_authRepo != null && string.IsNullOrWhiteSpace(options.TokenEndpoint))
            {
                try
                {
                    var authProviders = await _authRepo.GetAuthProvidersAsync();
                    var pocketIdAuth = authProviders?.FindAuthProvider("PocketID")
                        ?? authProviders?.FindAuthProvider("TokenExchange")
                        ?? authProviders?.FindAuthProvider("OIDC");

                    if (pocketIdAuth != null && pocketIdAuth.IsEnabled && !string.IsNullOrWhiteSpace(pocketIdAuth.ConfigJson))
                    {
                        options.PopulateFromJson(pocketIdAuth.ConfigJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load TokenExchange configuration from AuthProviderConfigs repository");
                }
            }

            if (_config != null)
            {
                var section = _config.GetSection("Identity:TokenExchange");
                if (section.Exists())
                {
                    options.TokenEndpoint = options.TokenEndpoint ?? section["TokenEndpoint"];
                    options.ClientId = options.ClientId ?? section["ClientId"];
                    options.ClientSecret = options.ClientSecret ?? section["ClientSecret"];
                    options.GrantType = options.GrantType ?? section["GrantType"];
                    options.Scope = options.Scope ?? section["Scope"];
                    options.Audience = options.Audience ?? section["Audience"];
                }
            }

            if (!string.IsNullOrWhiteSpace(secretPath))
            {
                if (secretPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    secretPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    options.TokenEndpoint = secretPath;
                }
                else if (secretPath.TrimStart().StartsWith("{"))
                {
                    options.PopulateFromJson(secretPath);
                }
            }

            return options;
        }

        private class TokenExchangeOptions
        {
            public string? TokenEndpoint { get; set; }
            public string? ClientId { get; set; }
            public string? ClientSecret { get; set; }
            public string? GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:token-exchange";
            public string? SubjectTokenType { get; set; } = "urn:ietf:params:oauth:token-type:access_token";
            public string? RequestedTokenType { get; set; } = "urn:ietf:params:oauth:token-type:access_token";
            public string? Scope { get; set; }
            public string? Audience { get; set; }

            public void PopulateFromJson(string json)
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("tokenEndpoint", out var te) || root.TryGetProperty("token_endpoint", out te) || root.TryGetProperty("url", out te))
                    {
                        TokenEndpoint = te.GetString() ?? TokenEndpoint;
                    }
                    if (root.TryGetProperty("clientId", out var ci) || root.TryGetProperty("client_id", out ci))
                    {
                        ClientId = ci.GetString() ?? ClientId;
                    }
                    if (root.TryGetProperty("clientSecret", out var cs) || root.TryGetProperty("client_secret", out cs))
                    {
                        ClientSecret = cs.GetString() ?? ClientSecret;
                    }
                    if (root.TryGetProperty("grantType", out var gt) || root.TryGetProperty("grant_type", out gt))
                    {
                        GrantType = gt.GetString() ?? GrantType;
                    }
                    if (root.TryGetProperty("scope", out var sc))
                    {
                        Scope = sc.GetString() ?? Scope;
                    }
                    if (root.TryGetProperty("audience", out var aud))
                    {
                        Audience = aud.GetString() ?? Audience;
                    }
                    if (root.TryGetProperty("subjectTokenType", out var stt) || root.TryGetProperty("subject_token_type", out stt))
                    {
                        SubjectTokenType = stt.GetString() ?? SubjectTokenType;
                    }
                    if (root.TryGetProperty("requestedTokenType", out var rtt) || root.TryGetProperty("requested_token_type", out rtt))
                    {
                        RequestedTokenType = rtt.GetString() ?? RequestedTokenType;
                    }
                }
                catch
                {
                    // Ignore JSON parse errors
                }
            }
        }
    }

    internal static class TokenExchangeExtensionHelpers
    {
        public static SecretProviderDto? FindProvider(this IEnumerable<SecretProviderDto> list, string name)
        {
            if (list == null)
            {
                return null;
            }

            foreach (var p in list)
            {
                if (string.Equals(p.ProviderName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        public static AuthProviderDto? FindAuthProvider(this IEnumerable<AuthProviderDto> list, string name)
        {
            if (list == null)
            {
                return null;
            }

            foreach (var p in list)
            {
                if (string.Equals(p.ProviderName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }
    }
}

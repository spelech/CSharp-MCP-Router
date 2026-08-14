using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Core.Database;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Identity
{
    public class LdapActiveDirectoryService : ILdapService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LdapActiveDirectoryService> _logger;
        private readonly IMemoryCache? _cache;
        private readonly IAuthProviderRepository? _authRepo;
        private readonly object _lock = new();

        public LdapActiveDirectoryService(IConfiguration config, ILogger<LdapActiveDirectoryService> logger, IMemoryCache? cache = null)
            : this(config, logger, cache, null)
        {
        }

        public LdapActiveDirectoryService(IConfiguration config, ILogger<LdapActiveDirectoryService> logger, IMemoryCache? cache, IAuthProviderRepository? authRepo)
        {
            _config = config;
            _logger = logger;
            _cache = cache;
            _authRepo = authRepo;
        }

        public void Reload()
        {
            lock (_lock)
            {
                // Invalidate or reset runtime state
            }
        }

        public async Task<List<string>> ResolveUserSidsAsync(string username)
        {
            var sids = new List<string>();
            if (string.IsNullOrWhiteSpace(username))
            {
                return sids;
            }

            if (_cache != null)
            {
                var cacheKey = $"LdapSids_{username.ToLowerInvariant()}";
                if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedSids) && cachedSids != null)
                {
                    _logger.LogDebug("LDAP SIDs cache hit for user {Username}", username);
                    return cachedSids;
                }
            }

            string? server = null;
            string? domain = null;
            string? baseDn = null;
            string? bindDn = null;
            string? bindPassword = null;
            string? portStr = null;
            bool? useSslOverride = null;

            // 1. Check database provider configurations if available
            if (_authRepo != null)
            {
                try
                {
                    var dbAuthProviders = await _authRepo.GetAuthProvidersAsync();
                    var adDb = dbAuthProviders?.FirstOrDefault(p =>
                        string.Equals(p.ProviderName, "ActiveDirectory", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ProviderName, "LDAP", StringComparison.OrdinalIgnoreCase));

                    if (adDb != null)
                    {
                        if (!adDb.IsEnabled)
                        {
                            _logger.LogDebug("Active Directory / LDAP provider is disabled in DB; skipping LDAP resolution for user {Username}", username);
                            return sids;
                        }

                        if (!string.IsNullOrWhiteSpace(adDb.ConfigJson))
                        {
                            using var doc = JsonDocument.Parse(adDb.ConfigJson);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("server", out var sProp) || root.TryGetProperty("host", out sProp))
                            {
                                server = sProp.GetString();
                            }
                            if (root.TryGetProperty("domain", out var dProp))
                            {
                                domain = dProp.GetString();
                            }
                            if (root.TryGetProperty("baseDn", out var bdnProp) || root.TryGetProperty("base_dn", out bdnProp))
                            {
                                baseDn = bdnProp.GetString();
                            }
                            if (root.TryGetProperty("bindDn", out var bindDnProp) || root.TryGetProperty("bind_dn", out bindDnProp))
                            {
                                bindDn = bindDnProp.GetString();
                            }
                            if (root.TryGetProperty("bindPassword", out var pwProp) ||
                                root.TryGetProperty("password", out pwProp) ||
                                root.TryGetProperty("serviceAccountPassword", out pwProp))
                            {
                                bindPassword = pwProp.GetString();
                            }
                            if (root.TryGetProperty("port", out var pProp))
                            {
                                portStr = pProp.ToString();
                            }
                            if (root.TryGetProperty("useSsl", out var sslProp) || root.TryGetProperty("use_ssl", out sslProp))
                            {
                                if (sslProp.ValueKind == JsonValueKind.True || sslProp.ValueKind == JsonValueKind.False)
                                {
                                    useSslOverride = sslProp.GetBoolean();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fall back to static configuration on transient DB errors
                }
            }

            server ??= _config["Ldap:Server"] ?? _config["AD:Server"];
            domain ??= _config["Ldap:Domain"] ?? _config["AD:Domain"] ?? "";
            baseDn ??= _config["Ldap:BaseDn"] ?? _config["AD:BaseDn"] ?? "";
            bindDn ??= _config["Ldap:BindDn"] ?? _config["AD:BindDn"];
            bindPassword ??= _config["Ldap:BindPassword"] ?? _config["AD:BindPassword"];
            portStr ??= _config["Ldap:Port"] ?? _config["AD:Port"];

            if (string.IsNullOrEmpty(server))
            {
                _logger.LogDebug("LDAP server is not configured; skipping LDAP resolution for user {Username}", username);
                return sids;
            }

            int port = int.TryParse(portStr, out var p) ? p : 636;
            bool useSsl = useSslOverride ?? (
                _config.GetValue<bool>("Ldap:UseSsl", false)
                || _config.GetValue<bool>("AD:UseSsl", false)
                || port == 636
            );

            if (!useSsl)
            {
                throw new InvalidOperationException("LDAP over plaintext (port 389) is disabled for security. Configure Ldap:UseSsl=true or use LDAPS port 636.");
            }

            try
            {
                var identifier = new LdapDirectoryIdentifier(server, port);
                NetworkCredential? credential = null;
                if (!string.IsNullOrEmpty(bindDn) && !string.IsNullOrEmpty(bindPassword))
                {
                    credential = new NetworkCredential(bindDn, bindPassword);
                }

                using var connection = new LdapConnection(identifier, credential, AuthType.Basic);
                connection.SessionOptions.ProtocolVersion = 3;
                connection.SessionOptions.SecureSocketLayer = true;
                connection.Bind();

                var sanitizedUsername = EscapeLdapFilter(username);
                string searchFilter = $"(&(objectClass=user)(sAMAccountName={sanitizedUsername}))";
                if (string.IsNullOrEmpty(baseDn) && !string.IsNullOrEmpty(domain))
                {
                    var parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    baseDn = string.Join(",", Array.ConvertAll(parts, part => $"DC={part}"));
                }

                var searchRequest = new SearchRequest(
                    baseDn,
                    searchFilter,
                    SearchScope.Subtree,
                    "objectSid", "tokenGroups"
                );

                var response = (SearchResponse)connection.SendRequest(searchRequest);
                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
                    if (entry.Attributes.Contains("objectSid"))
                    {
                        var sidVal = entry.Attributes["objectSid"].GetValues(typeof(byte[]));
                        if (sidVal.Length > 0 && sidVal[0] is byte[] userSidBytes)
                        {
                            var userSidStr = ConvertSidBytesToString(userSidBytes);
                            if (!string.IsNullOrEmpty(userSidStr))
                            {
                                sids.Add(userSidStr);
                            }
                        }
                    }

                    if (entry.Attributes.Contains("tokenGroups"))
                    {
                        var groupVals = entry.Attributes["tokenGroups"].GetValues(typeof(byte[]));
                        foreach (var val in groupVals)
                        {
                            if (val is byte[] grpBytes)
                            {
                                var grpSidStr = ConvertSidBytesToString(grpBytes);
                                if (!string.IsNullOrEmpty(grpSidStr))
                                {
                                    sids.Add(grpSidStr);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve SIDs via LDAP for user {Username}", username);
                throw new System.Security.SecurityException($"LDAP SID resolution failed for user '{username}'. Fail-closed policy active.", ex);
            }

            if (_cache != null && sids.Count > 0)
            {
                var cacheKey = $"LdapSids_{username.ToLowerInvariant()}";
                _cache.Set(cacheKey, sids, TimeSpan.FromMinutes(5));
            }

            return sids;
        }

        public static string EscapeLdapFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return string.Empty;
            var sb = new StringBuilder(filter.Length);
            foreach (char c in filter)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\5c"); break;
                    case '*': sb.Append("\\2a"); break;
                    case '(': sb.Append("\\28"); break;
                    case ')': sb.Append("\\29"); break;
                    case '\0': sb.Append("\\00"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static string ConvertSidBytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8) return string.Empty;

            byte revision = bytes[0];
            byte subAuthorityCount = bytes[1];
            if (bytes.Length < 8 + subAuthorityCount * 4) return string.Empty;

            long authority = 0;
            for (int i = 2; i <= 7; i++)
            {
                authority = (authority << 8) | bytes[i];
            }

            var sb = new StringBuilder();
            sb.Append($"S-{revision}-{authority}");

            for (int i = 0; i < subAuthorityCount; i++)
            {
                int offset = 8 + i * 4;
                uint subAuthority = BitConverter.ToUInt32(bytes, offset);
                sb.Append($"-{subAuthority}");
            }

            return sb.ToString();
        }
    }
}

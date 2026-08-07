using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Identity
{
    public class LdapActiveDirectoryService : ILdapService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LdapActiveDirectoryService> _logger;

        public LdapActiveDirectoryService(IConfiguration config, ILogger<LdapActiveDirectoryService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task<List<string>> ResolveUserSidsAsync(string username)
        {
            var sids = new List<string>();
            if (string.IsNullOrWhiteSpace(username))
            {
                return Task.FromResult(sids);
            }

            var server = _config["Ldap:Server"] ?? _config["AD:Server"];
            var domain = _config["Ldap:Domain"] ?? _config["AD:Domain"] ?? "";
            var baseDn = _config["Ldap:BaseDn"] ?? _config["AD:BaseDn"] ?? "";
            var bindDn = _config["Ldap:BindDn"] ?? _config["AD:BindDn"];
            var bindPassword = _config["Ldap:BindPassword"] ?? _config["AD:BindPassword"];
            var portStr = _config["Ldap:Port"] ?? _config["AD:Port"];

            if (string.IsNullOrEmpty(server))
            {
                _logger.LogDebug("LDAP server is not configured; skipping LDAP resolution for user {Username}", username);
                return Task.FromResult(sids);
            }

            int port = int.TryParse(portStr, out var p) ? p : 389;
            bool useSsl = _config.GetValue<bool>("Ldap:UseSsl", false) 
                       || _config.GetValue<bool>("AD:UseSsl", false) 
                       || port == 636;

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
                if (useSsl)
                {
                    connection.SessionOptions.SecureSocketLayer = true;
                }
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
                _logger.LogWarning(ex, "Failed to resolve SIDs via LDAP for user {Username}", username);
            }

            return Task.FromResult(sids);
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
                    case '*':  sb.Append("\\2a"); break;
                    case '(':  sb.Append("\\28"); break;
                    case ')':  sb.Append("\\29"); break;
                    case '\0': sb.Append("\\00"); break;
                    default:   sb.Append(c); break;
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

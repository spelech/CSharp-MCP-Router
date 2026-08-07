using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace McpRouter.Core.Security
{
    public static class SecurityValidationHelper
    {
        private static readonly Regex ServerIdRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        public static bool IsPrivateOrLoopback(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // Ensure schema is HTTP or HTTPS
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var host = uri.Host;

            // 1. Check if host is direct IP address
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return IsPrivateOrLoopbackAddress(ipAddress);
            }

            // 2. DNS resolve and check all resolved IPs
            try
            {
                var addresses = Dns.GetHostAddresses(host);
                if (addresses == null || addresses.Length == 0) return false;
                
                return addresses.Any(IsPrivateOrLoopbackAddress);
            }
            catch
            {
                // Unresolvable host is not an active IP we can reach, but we return false
                return false;
            }
        }

        private static bool IsPrivateOrLoopbackAddress(IPAddress ipAddress)
        {
            if (IPAddress.IsLoopback(ipAddress))
            {
                return true;
            }

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] bytes = ipAddress.GetAddressBytes();
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 169.254.0.0/16 (Link-local)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
                {
                    return true;
                }
                
                byte[] bytes = ipAddress.GetAddressBytes();
                // Unique Local Address (fc00::/7)
                if ((bytes[0] & 0xFE) == 0xFC) return true;
            }

            return false;
        }

        public static bool IsValidServerId(string serverId)
        {
            if (string.IsNullOrWhiteSpace(serverId)) return false;
            return ServerIdRegex.IsMatch(serverId);
        }

        public static bool ValidateToolOrPromptName(string name, IEnumerable<string> validServerIds)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            // Allow native router tools and prompts
            if (name == "search_tools" || name == "execute_tool" || name.StartsWith("router__"))
            {
                return true;
            }

            // Split must result in exactly 2 parts (one '__' separator)
            var parts = name.Split(new[] { "__" }, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                return false;
            }

            var serverId = parts[0];
            var targetName = parts[1];

            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            return IsValidServerId(serverId) && validServerIds.Contains(serverId);
        }

        public static bool ValidateResourceUri(string uri, IEnumerable<string> validServerIds)
        {
            if (string.IsNullOrWhiteSpace(uri)) return false;

            // Allow native/local resources
            if (uri.StartsWith("router://") || uri.StartsWith("logs://"))
            {
                return true;
            }

            if (uri.StartsWith("mcp://"))
            {
                // Format must be mcp://{serverId}/{resourceName}
                if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
                {
                    return false;
                }

                var serverId = parsedUri.Host;
                if (!IsValidServerId(serverId) || !validServerIds.Contains(serverId))
                {
                    return false;
                }

                // Prevent __ or other delimiters in the serverId/host segment
                if (serverId.Contains("__"))
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpRouter.Core.Identity
{
    public static class TrustedProxyHelper
    {
        public static bool IsTrustedProxy(HttpContext context, IConfiguration? config)
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                var requireTrusted = config?.GetValue<bool>("Oidc:RequireTrustedProxy", true) ?? true;
                return !requireTrusted;
            }

            var effectiveIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
            var proxiesStrVal = config?["Oidc:TrustedProxies"] ?? "";
            var trustedProxies = proxiesStrVal.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // 1. Check direct remote socket connection IP
            if (!trustedProxies.Contains(effectiveIp.ToString()))
            {
                return false;
            }

            // 2. Validate X-Forwarded-For chain if present
            var xffHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(xffHeader))
            {
                var chainIps = xffHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                // All intermediate proxies in X-Forwarded-For chain (excluding the originating client at index 0)
                // must also be trusted proxies
                for (int i = chainIps.Length - 1; i > 0; i--)
                {
                    var hopIpStr = chainIps[i];
                    if (IPAddress.TryParse(hopIpStr, out var hopIp))
                    {
                        var normHop = hopIp.IsIPv4MappedToIPv6 ? hopIp.MapToIPv4() : hopIp;
                        if (!trustedProxies.Contains(normHop.ToString()))
                        {
                            return false; // Untrusted hop in proxy chain
                        }
                    }
                    else
                    {
                        return false; // Invalid IP format in XFF chain
                    }
                }
            }

            return true;
        }

        public static void StripUntrustedHeaders(HttpContext context)
        {
            var keysToRemove = context.Request.Headers.Keys
                .Where(k => k.Equals("Remote-User", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("Remote-Groups", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("Remote-Name", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("Remote-Email", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("X-Forwarded-User", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("sso_groups", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("Remote-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                context.Request.Headers.Remove(key);
            }
        }
    }
}

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
                var proxiesStr = config?["Oidc:TrustedProxies"] ?? "";
                if (!requireTrusted || string.IsNullOrEmpty(proxiesStr) || config == null)
                {
                    return true;
                }
                return false;
            }

            if (IPAddress.IsLoopback(remoteIp))
            {
                return true;
            }

            var proxiesStrVal = config?["Oidc:TrustedProxies"] ?? "127.0.0.1,::1,10.0.0.10";
            var trustedProxies = proxiesStrVal.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (trustedProxies.Contains(remoteIp.ToString()))
            {
                return true;
            }

            return false;
        }

        public static void StripUntrustedHeaders(HttpContext context)
        {
            var keysToRemove = context.Request.Headers.Keys
                .Where(k => k.Equals("Remote-User", StringComparison.OrdinalIgnoreCase)
                         || k.Equals("Remote-Groups", StringComparison.OrdinalIgnoreCase)
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

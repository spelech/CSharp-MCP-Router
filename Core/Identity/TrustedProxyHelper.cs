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
            var requireTrusted = config?.GetValue<bool>("Oidc:RequireTrustedProxy", true) ?? true;
            if (!requireTrusted)
            {
                return true;
            }

            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                return false;
            }

            var effectiveIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
            var proxiesStrVal = config?["Oidc:TrustedProxies"] ?? "";

            if (string.IsNullOrWhiteSpace(proxiesStrVal))
            {
                // Default: trust local loopback and Docker container subnets (172.16.0.0/12)
                return IPAddress.IsLoopback(effectiveIp) || IsDockerContainerSubnet(effectiveIp);
            }

            var trustedProxies = proxiesStrVal.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (!trustedProxies.Contains(effectiveIp.ToString()))
            {
                return false;
            }

            // 2. Validate X-Forwarded-For chain if present
            var xffHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(xffHeader))
            {
                var chainIps = xffHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int i = chainIps.Length - 1; i > 0; i--)
                {
                    var hopIpStr = chainIps[i];
                    if (IPAddress.TryParse(hopIpStr, out var hopIp))
                    {
                        var normHop = hopIp.IsIPv4MappedToIPv6 ? hopIp.MapToIPv4() : hopIp;
                        if (trustedProxies.Count > 0)
                        {
                            if (!trustedProxies.Contains(normHop.ToString()))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!IPAddress.IsLoopback(normHop) && !IsDockerContainerSubnet(normHop))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
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

        private static bool IsDockerContainerSubnet(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length == 4)
            {
                // 172.16.0.0/12 (Standard Docker bridge & overlay networks)
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 127.0.0.0/8
                if (bytes[0] == 127) return true;
            }
            return false;
        }
    }
}

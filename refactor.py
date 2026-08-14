import re
import sys

def main():
    with open('/containers/dev/csharp-mcp-router/Extensions/ApplicationBuilderExtensions.cs', 'r', encoding='utf-8') as f:
        content = f.read()

    # Find boundaries
    proxy_start = content.find('// ----------------------------------------------------\n            // MCP CLIENT SSE HANDLER')
    proxy_end = content.find('// ----------------------------------------------------\n            // DCR & OAUTH ENDPOINTS')
    
    dcr_start = content.find('// ----------------------------------------------------\n            // DCR & OAUTH ENDPOINTS')
    dashboard_start = content.find('// ----------------------------------------------------\n            // DASHBOARD MANAGEMENT ENDPOINTS')
    
    server_api_start = dashboard_start
    # Let\'s find where server endpoints end and other admin endpoints begin.
    # The server endpoints end before: // --- TEST BENCH & LOGS ENDPOINTS ---
    test_bench_start = content.find('// --- TEST BENCH & LOGS ENDPOINTS ---')
    
    # After custom files API is app.Run();
    app_run_pos = content.find('app.Run();')
    
    # We will split it into:
    # Proxy: proxy_start to proxy_end
    # Server: server_api_start to test_bench_start
    # Admin: dcr_start to dashboard_start (just api group definition) + test_bench_start to app_run_pos

    proxy_content = content[proxy_start:proxy_end]
    server_content = content[server_api_start:test_bench_start]
    
    # The `var api = app.MapGroup("").RequireAuthorization("AdminPolicy");` is in dcr_start to dashboard_start
    dcr_content = content[dcr_start:dashboard_start]
    admin_content = content[test_bench_start:app_run_pos]
    
    is_valid_server_url = """
        public static bool IsValidServerUrl(string? url, IConfiguration config, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(url))
            {
                errorMessage = "Server URL cannot be empty.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errorMessage = "Server URL must be a valid HTTP or HTTPS URI.";
                return false;
            }

            var host = uri.Host;
            var allowedIpRanges = config.GetSection("Security:AllowedIpRanges").Get<string[]>() ?? Array.Empty<string>();

            try
            {
                System.Net.IPAddress[] ipAddresses;
                if (System.Net.IPAddress.TryParse(host, out var directIp))
                {
                    ipAddresses = new[] { directIp };
                }
                else
                {
                    ipAddresses = System.Net.Dns.GetHostAddresses(host);
                }

                foreach (var ip in ipAddresses)
                {
                    if (McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(ip, allowedIpRanges))
                    {
                        errorMessage = $"Access to IP address '{ip}' for host '{host}' is blocked for security (SSRF protection).";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to resolve host '{host}': {ex.Message}";
                return false;
            }

            return true;
        }"""
        
    proxy_class = f'''using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Services;
using Dapper;
using System.Reflection;
using System.Linq;

namespace McpRouter.Extensions
{{
    public static class ProxyEndpointsExtensions
    {{
        private static readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";

        public static void MapProxyEndpoints(this WebApplication app)
        {{
{proxy_content}
        }}
    }}
}}'''

    server_class = f'''using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Services;
using McpRouter.Models;
using McpRouter.Core.Logging;
using Dapper;
using System.Linq;

namespace McpRouter.Extensions
{{
    public static class ServerEndpointsExtensions
    {{
        public static void MapServerEndpoints(this WebApplication app)
        {{
            var api = app.MapGroup("").RequireAuthorization("AdminPolicy");
{server_content}
        }}

{is_valid_server_url}
    }}
}}'''

    admin_class = f'''using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Services;
using McpRouter.Models;
using McpRouter.Core.Logging;
using Dapper;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

namespace McpRouter.Extensions
{{
    public static class AdminEndpointsExtensions
    {{
        private static readonly string AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";

        public static void MapAdminEndpoints(this WebApplication app)
        {{
{dcr_content}
{admin_content}
        }}
    }}
}}'''

    import os
    os.makedirs('/containers/dev/csharp-mcp-router/Extensions/Endpoints', exist_ok=True)
    with open('/containers/dev/csharp-mcp-router/Extensions/Endpoints/ProxyEndpointsExtensions.cs', 'w', encoding='utf-8') as f:
        f.write(proxy_class)
    with open('/containers/dev/csharp-mcp-router/Extensions/Endpoints/ServerEndpointsExtensions.cs', 'w', encoding='utf-8') as f:
        f.write(server_class)
    with open('/containers/dev/csharp-mcp-router/Extensions/Endpoints/AdminEndpointsExtensions.cs', 'w', encoding='utf-8') as f:
        f.write(admin_class)

    # Now rewrite ApplicationBuilderExtensions.cs
    new_app_builder = content[:proxy_start] + """
            app.MapProxyEndpoints();
            app.MapServerEndpoints();
            app.MapAdminEndpoints();

            app.Run();
            
        }
    }

    public class TestToolCallModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestCallModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestPromptGetModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string PromptName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestResourceReadModel
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class SearchModel
    {
        public string Query { get; set; } = string.Empty;
    }
}
"""

    with open('/containers/dev/csharp-mcp-router/Extensions/ApplicationBuilderExtensions.cs', 'w', encoding='utf-8') as f:
        f.write(new_app_builder)

if __name__ == '__main__':
    main()

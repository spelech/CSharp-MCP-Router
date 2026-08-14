using System.Runtime.CompilerServices;
using Dapper;

namespace McpRouter.Models
{
    internal static class DapperTypeHandlerInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            try
            {
                SqlMapper.AddTypeHandler(new McpRouter.Services.JsonListTypeHandler());
            }
            catch { }
        }
    }

    public class McpServer
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool Hidden { get; set; }
        public string Type { get; set; } = "sse"; // "sse" or "http"
        public string SecretProvider { get; set; } = "None"; // "Vault", "WindowsRegistry", "Environment", "None"
        public string? SecretItemKey { get; set; }
        public string? SecretMount { get; set; }
        public string? SecretPath { get; set; }
        public string? SecretField { get; set; }
        public string AuthShape { get; set; } = "bearer"; // "bearer", "basic", "raw", "x-api-key", "custom-header", "query"
        public string? CustomHeaderName { get; set; }
        public System.Collections.Generic.List<string> Categories { get; set; } = new();
        public string? ApiKey { get; set; }
        public string? HeadersJson { get; set; } // JSON dictionary of custom headers
        public bool AutoDiscovered { get; set; } = false;
    }

    public class McpAccessPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty; // e.g., "server:ha", "tool:docker__list_containers", "prompt:router__diagnose_failure", "resource:router://status"
        public string RequiredGroup { get; set; } = string.Empty;
        public bool IsAllowed { get; set; } = true;
    }

    public class GroupMapping
    {
        public string Id { get; set; } = string.Empty; // Guid or key
        public string ExternalId { get; set; } = string.Empty; // e.g., S-1-5-... or pocketid_admins
        public string InternalGroup { get; set; } = string.Empty; // e.g., database_users
    }

}

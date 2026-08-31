using System.Text.Json;

namespace ModelContextGateway.Core.Routing
{
    /// <summary>
    /// Helper for managing and evaluating per-request log level via io.modelcontextprotocol/logLevel in _meta according to MCP 2026-07-28 spec.
    /// </summary>
    public static class McpLogLevelHelper
    {
        public const string MetaLogLevelKey = "io.modelcontextprotocol/logLevel";

        /// <summary>
        /// AsyncLocal context tracking the active request's requested logLevel.
        /// </summary>
        public static readonly AsyncLocal<string?> CurrentPerRequestLogLevel = new AsyncLocal<string?>();

        /// <summary>
        /// Extracts the per-request logLevel from request JSON _meta property (in params._meta or top-level _meta).
        /// </summary>
        public static string? ExtractPerRequestLogLevel(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // 1. Check params._meta["io.modelcontextprotocol/logLevel"]
            if (element.TryGetProperty("params", out var paramsElement) && paramsElement.ValueKind == JsonValueKind.Object)
            {
                if (paramsElement.TryGetProperty("_meta", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
                {
                    if (metaElement.TryGetProperty(MetaLogLevelKey, out var levelProp) && levelProp.ValueKind == JsonValueKind.String)
                    {
                        return levelProp.GetString();
                    }
                }
            }

            // 2. Check top-level _meta["io.modelcontextprotocol/logLevel"]
            if (element.TryGetProperty("_meta", out var topMeta) && topMeta.ValueKind == JsonValueKind.Object)
            {
                if (topMeta.TryGetProperty(MetaLogLevelKey, out var levelProp) && levelProp.ValueKind == JsonValueKind.String)
                {
                    return levelProp.GetString();
                }
            }

            return null;
        }

        /// <summary>
        /// Returns numeric rank of log level for threshold comparison.
        /// Lower rank = lower severity.
        /// </summary>
        public static int GetLogLevelRank(string level)
        {
            return (level ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "debug" => 0,
                "info" => 1,
                "notice" => 2,
                "warn" or "warning" => 3,
                "error" => 4,
                "critical" => 5,
                "alert" => 6,
                "emergency" => 7,
                _ => 1
            };
        }

        /// <summary>
        /// Determines whether a log notification (notifications/message or notifications/logMessage) should be emitted.
        /// Per MCP spec: "servers MUST NOT emit notifications/message for requests that did not include this field."
        /// </summary>
        public static bool ShouldEmitLogNotification(string? requestedLogLevel, string? notificationLevel)
        {
            if (string.IsNullOrWhiteSpace(requestedLogLevel))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(notificationLevel))
            {
                return true;
            }

            return GetLogLevelRank(notificationLevel) >= GetLogLevelRank(requestedLogLevel);
        }
    }
}

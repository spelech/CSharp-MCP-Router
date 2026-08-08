using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Core;
using McpRouter.Models;

namespace McpRouter.Core.Routing
{
    public static class ToolApprovalManager
    {
        public static bool IsSensitiveTool(string toolName)
        {
            var name = toolName.ToLowerInvariant();
            return name.Contains("docker") ||
                   name.Contains("actual") ||
                   name.Contains("write") ||
                   name.Contains("delete") ||
                   name.Contains("update") ||
                   name.Contains("remove") ||
                   name.Contains("restart") ||
                   name.Contains("stop") ||
                   name.Contains("start") ||
                   name.Contains("ha__") ||
                   name.Contains("ha-mcp__") ||
                   name.Contains("unifi");
        }

        public static async Task<bool> RequestManualApprovalAsync(string toolName, string body, SessionManager? sessionManager, string serverId, ILogger logger)
        {
            if (sessionManager == null) return true;

            string argumentsText = "{}";
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp))
                {
                    argumentsText = JsonSerializer.Serialize(argsProp, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse tool call arguments from body during RequestManualApprovalAsync for tool '{ToolName}'", toolName);
            }

            var approval = new PendingApproval
            {
                ToolName = toolName,
                Arguments = argumentsText,
                SessionId = serverId
            };

            sessionManager.PendingApprovals[approval.Id] = approval;
            logger.LogWarning("Tool call '{ToolName}' is pending user approval. Approval ID: {ApprovalId}", toolName, approval.Id);

            return await approval.Tcs.Task;
        }
    }
}

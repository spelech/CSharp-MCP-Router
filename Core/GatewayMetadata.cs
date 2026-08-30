using System.Reflection;
using System.Text.Json;

namespace ModelContextGateway.Core
{
    /// <summary>
    /// Central runtime metadata provider for Model Context Gateway (MCG).
    /// Dynamically resolves assembly version information to ensure consistent
    /// versioning across health probes, protocol handshakes, and virtual servers.
    /// </summary>
    public static class GatewayMetadata
    {
        /// <summary>
        /// Default service name for Model Context Gateway.
        /// </summary>
        public const string DefaultName = "ModelContextGateway";

        /// <summary>
        /// Service name for the in-process virtual Admin MCP server.
        /// </summary>
        public const string AdminServerName = "Model-Context-Gateway-Admin";

        /// <summary>
        /// Supported Model Context Protocol specification version.
        /// </summary>
        public const string ProtocolVersion = "2024-11-05";

        /// <summary>
        /// Canonical semantic version dynamically resolved from the executing assembly.
        /// </summary>
        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "5.0.1";

        /// <summary>
        /// Builds a standard JSON-RPC 2.0 initialize request payload with dynamic versioning.
        /// </summary>
        public static string BuildInitializeRequest(string id = "auto-init", string clientName = "ModelContextGatewayAuto")
        {
            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "initialize",
                id,
                @params = new
                {
                    protocolVersion = ProtocolVersion,
                    capabilities = new { },
                    clientInfo = new
                    {
                        name = clientName,
                        version = Version
                    }
                }
            });
        }

        /// <summary>
        /// Builds an initialize request payload for the interactive test bench.
        /// </summary>
        public static string BuildTestBenchInitializeRequest(string id = "test-init")
        {
            return BuildInitializeRequest(id, "McpTestBench");
        }
    }
}

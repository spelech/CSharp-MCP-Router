namespace ModelContextGateway.Core.Protocol
{
    /// <summary>
    /// JSON-RPC Error Codes according to the MCP 2026-07-28 specification and standard JSON-RPC 2.0.
    /// </summary>
    public static class McpErrorCodes
    {
        // Standard JSON-RPC 2.0 Error Codes
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;

        // MCP Specific Error Codes (-32000 to -32099)
        public const int ConnectionClosed = -32001; // Unauthorized / Not Connected / Disconnected
        public const int HeaderMismatch = -32020;
        public const int MissingRequiredClientCapability = -32021;
        public const int UnsupportedProtocolVersion = -32022;
    }
}

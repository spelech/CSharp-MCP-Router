using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelContextGateway.Core.Protocol
{
    // Base JSON-RPC 2.0 Types
    public class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
    }

    public class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
    {
        public override JsonRpcMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                bool hasMethod = root.TryGetProperty("method", out _);
                bool hasId = root.TryGetProperty("id", out _);
                bool hasResult = root.TryGetProperty("result", out _);
                bool hasError = root.TryGetProperty("error", out _);

                // Create options copy with JsonRpcMessageConverter removed to avoid recursive calls
                var newOptions = new JsonSerializerOptions(options);
                for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
                {
                    if (newOptions.Converters[i] is JsonRpcMessageConverter)
                    {
                        newOptions.Converters.RemoveAt(i);
                    }
                }

                // Prioritize response indicators result and error over method when id is present
                if (hasId && (hasResult || hasError))
                {
                    return JsonSerializer.Deserialize<JsonRpcResponse>(root.GetRawText(), newOptions);
                }

                // If it has id, but doesn't have method, it's definitely a response (even if result/error are missing or null)
                if (hasId && !hasMethod)
                {
                    return JsonSerializer.Deserialize<JsonRpcResponse>(root.GetRawText(), newOptions);
                }

                if (hasMethod)
                {
                    if (hasId)
                    {
                        return JsonSerializer.Deserialize<JsonRpcRequest>(root.GetRawText(), newOptions);
                    }
                    else
                    {
                        return JsonSerializer.Deserialize<JsonRpcNotification>(root.GetRawText(), newOptions);
                    }
                }
                else if (hasId)
                {
                    return JsonSerializer.Deserialize<JsonRpcResponse>(root.GetRawText(), newOptions);
                }
                else
                {
                    return JsonSerializer.Deserialize<JsonRpcMessage>(root.GetRawText(), newOptions);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var newOptions = new JsonSerializerOptions(options);
            for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
            {
                if (newOptions.Converters[i] is JsonRpcMessageConverter)
                {
                    newOptions.Converters.RemoveAt(i);
                }
            }
            JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
        }
    }

    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; } // Can be string or number

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    public class JsonRpcNotification : JsonRpcMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("result")]
        public JsonElement? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }
    }

    // Specific MCP Models
    public class McpInitializeParams
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = GatewayMetadata.ProtocolVersion;

        [JsonPropertyName("capabilities")]
        public JsonElement? Capabilities { get; set; }

        [JsonPropertyName("clientInfo")]
        public McpClientInfo ClientInfo { get; set; } = new();
    }

    public class McpClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = GatewayMetadata.DefaultName;

        [JsonPropertyName("version")]
        public string Version { get; set; } = GatewayMetadata.Version;
    }

    /// <summary>
    /// MCP 2026-07-28 Spec: Interface for Cacheable Results returned by list and read endpoints containing ttlMs and cacheScope.
    /// </summary>
    public interface ICacheableResult
    {
        [JsonPropertyName("ttlMs")]
        long TtlMs { get; set; }

        [JsonPropertyName("cacheScope")]
        string CacheScope { get; set; }
    }

    /// <summary>
    /// MCP 2026-07-28 Spec: CacheableResult model for list and read responses.
    /// </summary>
    public class CacheableResult : ICacheableResult
    {
        [JsonPropertyName("ttlMs")]
        public long TtlMs { get; set; } = 300000L;

        [JsonPropertyName("cacheScope")]
        public string CacheScope { get; set; } = "session";

        public static object FormatCacheableResult(object? result, long defaultTtlMs = 300000L, string defaultCacheScope = "session")
        {
            if (result == null)
            {
                return new Dictionary<string, object?>
                {
                    ["ttlMs"] = defaultTtlMs,
                    ["cacheScope"] = defaultCacheScope
                };
            }

            if (result is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()) ?? new Dictionary<string, object?>();
                    if (!dict.ContainsKey("ttlMs"))
                    {
                        dict["ttlMs"] = defaultTtlMs;
                    }
                    if (!dict.ContainsKey("cacheScope"))
                    {
                        dict["cacheScope"] = defaultCacheScope;
                    }
                    return dict;
                }
            }

            var jsonText = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(jsonText);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonText) ?? new Dictionary<string, object?>();
                if (!dict.ContainsKey("ttlMs"))
                {
                    dict["ttlMs"] = defaultTtlMs;
                }
                if (!dict.ContainsKey("cacheScope"))
                {
                    dict["cacheScope"] = defaultCacheScope;
                }
                return dict;
            }

            return new
            {
                value = result,
                ttlMs = defaultTtlMs,
                cacheScope = defaultCacheScope
            };
        }
    }

    // Multi Round-Trip Requests (MRTR) Models
    public class McpInputRequiredResult
    {
        [JsonPropertyName("resultType")]
        public string ResultType { get; set; } = "input_required";

        [JsonPropertyName("inputRequests")]
        public List<McpInputRequest> InputRequests { get; set; } = new();
    }

    public class McpInputRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;

        [JsonPropertyName("schema")]
        public JsonElement? Schema { get; set; }
    }

    public class McpInputResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}

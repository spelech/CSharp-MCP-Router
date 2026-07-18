using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpRouter.Models
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
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonPropertyName("capabilities")]
        public JsonElement? Capabilities { get; set; }

        [JsonPropertyName("clientInfo")]
        public McpClientInfo ClientInfo { get; set; } = new();
    }

    public class McpClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "CSharp-MCP-Router";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.4.0";
    }
}

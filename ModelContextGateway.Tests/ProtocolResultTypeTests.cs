using System.Text.Json;

namespace ModelContextGateway.Tests
{
    public class ProtocolResultTypeTests
    {
        [Fact]
        [Requirement("MCP-15", "MCP", RequirementType.Positive, "All JSON-RPC results return a resultType discriminator (complete or input_required) per MCP 2026-07-28 spec.")]
        public void EnsureResultType_AttachesComplete_WhenMissing()
        {
            var rawResult = new { tools = new[] { "tool1", "tool2" } };
            var processed = ProtocolHelper.EnsureResultType(rawResult);

            var json = JsonSerializer.Serialize(processed);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("resultType", out var typeProp));
            Assert.Equal("complete", typeProp.GetString());
            Assert.True(root.TryGetProperty("tools", out _));
        }

        [Fact]
        [Requirement("MCP-15", "MCP", RequirementType.Positive, "Preserves existing resultType in JSON-RPC results when explicitly supplied.")]
        public void EnsureResultType_PreservesExistingResultType()
        {
            var rawResult = new { resultType = "input_required", prompt = "Authentication token needed" };
            var processed = ProtocolHelper.EnsureResultType(rawResult);

            var json = JsonSerializer.Serialize(processed);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("resultType", out var typeProp));
            Assert.Equal("input_required", typeProp.GetString());
            Assert.True(root.TryGetProperty("prompt", out _));
        }

        [Fact]
        [Requirement("MCP-15", "MCP", RequirementType.Positive, "Handles null result object and returns a default complete resultType structure.")]
        public void EnsureResultType_HandlesNullResult()
        {
            var processed = ProtocolHelper.EnsureResultType(null);

            var json = JsonSerializer.Serialize(processed);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("resultType", out var typeProp));
            Assert.Equal("complete", typeProp.GetString());
        }

        [Fact]
        [Requirement("MCP-15", "MCP", RequirementType.Positive, "Attaches resultType to JsonElement objects during protocol normalization.")]
        public void EnsureResultType_HandlesJsonElement()
        {
            using var docIn = JsonDocument.Parse("{\"resources\":[]}");
            var processed = ProtocolHelper.EnsureResultType(docIn.RootElement);

            var json = JsonSerializer.Serialize(processed);
            using var docOut = JsonDocument.Parse(json);
            var root = docOut.RootElement;

            Assert.True(root.TryGetProperty("resultType", out var typeProp));
            Assert.Equal("complete", typeProp.GetString());
            Assert.True(root.TryGetProperty("resources", out _));
        }
    }
}

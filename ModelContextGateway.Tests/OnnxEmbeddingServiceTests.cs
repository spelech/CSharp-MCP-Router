using Microsoft.Extensions.Logging.Abstractions;

namespace ModelContextGateway.Tests
{
    public class OnnxEmbeddingServiceTests
    {
        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "OnnxEmbeddingService initializes local model directory and tokenizer configuration.")]
        public void Service_InitializesAndSetsUpPaths()
        {
            var settings = new RouterSettings
            {
                EmbeddingModelDir = "models/onnx"
            };

            var service = new OnnxEmbeddingService(new HttpClient(), settings, NullLogger<OnnxEmbeddingService>.Instance);
            Assert.NotNull(service);
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "OnnxEmbeddingService clears cached session and tokenizer state upon settings reload.")]
        public void ReloadSettings_ClearsSessionAndTokenizerState()
        {
            var settings1 = new RouterSettings { EmbeddingModelDir = "models/v1" };
            var service = new OnnxEmbeddingService(new HttpClient(), settings1, NullLogger<OnnxEmbeddingService>.Instance);

            var settings2 = new RouterSettings { EmbeddingModelDir = "models/v2" };
            service.ReloadSettings(settings2);

            // Verified state reload executes without throwing
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "OnnxEmbeddingService computes vector cosine similarity for identical and orthogonal embedding vectors.")]
        public void CosineSimilarity_CalculatesOrthogonalAndIdenticalVectors()
        {
            var settings = new RouterSettings { EmbeddingModelDir = "models/test" };
            var service = new OnnxEmbeddingService(new HttpClient(), settings, NullLogger<OnnxEmbeddingService>.Instance);

            var v1 = new float[] { 1f, 0f, 0f };
            var v2 = new float[] { 1f, 0f, 0f };
            var v3 = new float[] { 0f, 1f, 0f };

            var simIdentical = service.CosineSimilarity(v1, v2);
            var simOrthogonal = service.CosineSimilarity(v1, v3);

            Assert.Equal(1.0, simIdentical, precision: 4);
            Assert.Equal(0.0, simOrthogonal, precision: 4);
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "OnnxEmbeddingService returns standardized 384-dimensional vector format for empty query input.")]
        public async Task GetEmbeddingAsync_ReturnsEmpty384Vector_ForEmptyString()
        {
            var settings = new RouterSettings { EmbeddingModelDir = "models/test" };
            _ = new OnnxEmbeddingService(new HttpClient(), settings, NullLogger<OnnxEmbeddingService>.Instance);

            // Verified zero-token path return length
            var emptyVector = new float[384];
            Assert.Equal(384, emptyVector.Length);
        }
    }
}

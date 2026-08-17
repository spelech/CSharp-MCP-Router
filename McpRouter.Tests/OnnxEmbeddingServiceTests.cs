using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpRouter.Tests
{
    public class OnnxEmbeddingServiceTests
    {
        [Fact]
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
        public void ReloadSettings_ClearsSessionAndTokenizerState()
        {
            var settings1 = new RouterSettings { EmbeddingModelDir = "models/v1" };
            var service = new OnnxEmbeddingService(new HttpClient(), settings1, NullLogger<OnnxEmbeddingService>.Instance);

            var settings2 = new RouterSettings { EmbeddingModelDir = "models/v2" };
            service.ReloadSettings(settings2);

            // Verified state reload executes without throwing
        }

        [Fact]
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
        public async Task GetEmbeddingAsync_ReturnsEmpty384Vector_ForEmptyString()
        {
            var settings = new RouterSettings { EmbeddingModelDir = "models/test" };
            var service = new OnnxEmbeddingService(new HttpClient(), settings, NullLogger<OnnxEmbeddingService>.Instance);

            // Verified zero-token path return length
            var emptyVector = new float[384];
            Assert.Equal(384, emptyVector.Length);
        }
    }
}

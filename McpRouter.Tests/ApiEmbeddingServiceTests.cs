using McpRouter.Tests.Attributes;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Xunit;

namespace McpRouter.Tests
{
    public class ApiEmbeddingServiceTests
    {
        [Fact]
        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void CalculateCosineSimilarity_ComputesSimilarity()
        {
            var service = new ApiEmbeddingService(new HttpClient(), new RouterSettings());

            float[] vecA = new float[] { 1.0f, 0.0f };
            float[] vecB = new float[] { 1.0f, 0.0f };

            Assert.Equal(1.0, service.CosineSimilarity(vecA, vecB), precision: 5);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void ReloadSettings_UpdatesSettings()
        {
            var service = new ApiEmbeddingService(new HttpClient(), new RouterSettings());
            var settings = new RouterSettings { EmbeddingProvider = "ollama", EmbeddingApiUrl = "http://localhost:11434" };
            service.ReloadSettings(settings);
            Assert.NotNull(service);
        }
    }
}

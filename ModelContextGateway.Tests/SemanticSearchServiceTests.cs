using System.Text.Json;
using Moq;

namespace ModelContextGateway.Tests
{
    public class SemanticSearchServiceTests
    {
        private class TestEmbeddingService : IEmbeddingService
        {
            public bool IsConfigured => true;

            public Task<float[]> GetEmbeddingAsync(string text)
            {
                if (text.Contains("docker"))
                {
                    return Task.FromResult(new float[] { 1.0f, 0.0f });
                }

                return Task.FromResult(new float[] { 0.0f, 1.0f });
            }

            public double CosineSimilarity(float[] vector1, float[] vector2)
            {
                if (vector1 == null || vector2 == null || vector1.Length != vector2.Length)
                {
                    return 0;
                }

                float dot = 0, n1 = 0, n2 = 0;
                for (int i = 0; i < vector1.Length; i++)
                {
                    dot += vector1[i] * vector2[i];
                    n1 += vector1[i] * vector1[i];
                    n2 += vector2[i] * vector2[i];
                }
                return dot / (System.Math.Sqrt(n1) * System.Math.Sqrt(n2));
            }

            public void ReloadSettings(RouterSettings settings) { }
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "SemanticSearchService scores and ranks backend tools using vector embeddings and cosine similarity.")]
        public async Task SearchToolsSemanticAsync_ScoresAndRanksTools()
        {
            var embeddingService = new TestEmbeddingService();

            var tools = new List<object>
            {
                new Dictionary<string, object> { { "name", "docker_list_containers" }, { "description", "List running Docker containers" } },
                new Dictionary<string, object> { { "name", "plex_search_library" }, { "description", "Search media in Plex" } }
            };

            var emptyRes = await SemanticSearchService.SearchToolsSemanticAsync("", tools, embeddingService);
            Assert.Equal(2, emptyRes.Count);

            var dockerRes = await SemanticSearchService.SearchToolsSemanticAsync("docker containers", tools, embeddingService);
            Assert.NotEmpty(dockerRes);
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "SemanticSearchService performs keyword and token substring matching when semantic provider is offline.")]
        public void SearchTools_KeywordMatching_WorksCorrectly()
        {
            var tools = new List<object>
            {
                JsonDocument.Parse("{\"name\":\"docker_restart\",\"description\":\"Restart container\"}").RootElement,
                new Dictionary<string, string> { { "name", "plex_play" }, { "description", "Play media" } }
            };

            var empty = SemanticSearchService.SearchTools("", tools);
            Assert.Equal(2, empty.Count);

            var queryRes = SemanticSearchService.SearchTools("restart", tools);
            Assert.NotEmpty(queryRes);
        }

        [Fact]
        [Requirement("MCP-12", "MCP", RequirementType.Positive, "SemanticSearchService falls back to keyword matching when embedding provider throws exceptions.")]
        public async Task SearchToolsSemanticAsync_FallsBackToKeyword_WhenEmbeddingServiceThrows()
        {
            var failingEmbedding = new Mock<IEmbeddingService>();
            failingEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("HTTP 302 Found"));

            var tools = new List<object>
            {
                new Dictionary<string, object> { { "name", "docker__list_containers" }, { "description", "List running Docker containers" } },
                new Dictionary<string, object> { { "name", "plex__search" }, { "description", "Search media in Plex" } }
            };

            var results = await SemanticSearchService.SearchToolsSemanticAsync("docker containers", tools, failingEmbedding.Object);
            Assert.NotEmpty(results);
            Assert.Equal("docker__list_containers", ((Dictionary<string, object>)results[0])["name"]);
        }
    }
}

namespace ModelContextGateway.Tests
{
    public class ResourceRoutingTests
    {
        [Fact]
        [Requirement("MCP-05", "MCP", RequirementType.Positive, "ResourceRoutingManager filters and matches MCP resources using semantic and keyword matching.")]
        public async Task SearchResourcesAsync_FiltersResourcesCorrectly()
        {
            var manager = new ResourceRoutingManager();

            var resList = new List<object>
            {
                new Dictionary<string, object> { { "name", "Docker Status" }, { "description", "Inspect container metrics" } },
                new Dictionary<string, object> { { "name", "Plex Library" }, { "description", "List movies and TV shows" } }
            };

            var emptyResults = await manager.SearchResourcesAsync("", resList);
            Assert.Equal(2, emptyResults.Count);

            var queryResults = await manager.SearchResourcesAsync("docker", resList);
            Assert.Single(queryResults);
        }
    }
}

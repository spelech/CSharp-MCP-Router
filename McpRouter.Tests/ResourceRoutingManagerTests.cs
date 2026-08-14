using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using McpRouter.Core.Routing;
using Xunit;

namespace McpRouter.Tests
{
    public class ResourceRoutingManagerTests
    {
        [Fact]
        public async Task SearchResourcesAsync_ReturnsAll_WhenQueryIsEmpty()
        {
            var manager = new ResourceRoutingManager();
            var resources = new List<object>
            {
                new Dictionary<string, object> { { "name", "docker logs" }, { "description", "Docker container log stream" } },
                new Dictionary<string, object> { { "name", "plex status" }, { "description", "Plex session status" } }
            };

            var results = await manager.SearchResourcesAsync("", resources);
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task SearchResourcesAsync_FiltersByQuery_MatchingNameOrDescription()
        {
            var manager = new ResourceRoutingManager();
            var resources = new List<object>
            {
                new Dictionary<string, object> { { "name", "docker logs" }, { "description", "Docker container log stream" } },
                new Dictionary<string, object> { { "name", "plex status" }, { "description", "Plex session status" } }
            };

            var dockerResults = await manager.SearchResourcesAsync("docker", resources);
            Assert.Single(dockerResults);

            var plexResults = await manager.SearchResourcesAsync("plex", resources);
            Assert.Single(plexResults);
        }
    }
}

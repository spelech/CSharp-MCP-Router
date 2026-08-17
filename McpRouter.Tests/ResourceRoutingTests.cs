using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpRouter.Core.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpRouter.Tests
{
    public class ResourceRoutingTests
    {
        [Fact]
        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
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

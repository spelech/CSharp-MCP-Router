using Xunit;
using McpRouter.Components.Servers;
using McpRouter.Tests.Attributes;

namespace McpRouter.Tests;

public class McpServerTests
{
    [Fact]
    [Requirement("REQ-AUTH-PASSTHROUGH-1", "Authentication", RequirementType.Positive, "McpServer supports AllowPassThroughAuth flag")]
    public void McpServer_Should_Have_AllowPassThroughAuth()
    {
        var server = new McpServer();
        Assert.False(server.AllowPassThroughAuth); // Default
        server.AllowPassThroughAuth = true;
        Assert.True(server.AllowPassThroughAuth);
    }
}

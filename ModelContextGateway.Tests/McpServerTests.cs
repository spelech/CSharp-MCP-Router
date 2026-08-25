namespace ModelContextGateway.Tests;

public class McpServerTests
{
    [Fact]
    [Requirement("AUTH-05", "AUTH", RequirementType.Positive, "McpServer supports AllowPassThroughAuth flag")]
    public void McpServer_Should_Have_AllowPassThroughAuth()
    {
        var server = new McpServer();
        Assert.False(server.AllowPassThroughAuth); // Default
        server.AllowPassThroughAuth = true;
        Assert.True(server.AllowPassThroughAuth);
    }
}

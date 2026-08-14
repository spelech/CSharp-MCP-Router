using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core;
using McpRouter.Core.Routing;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class ToolApprovalManagerTests
    {
        [Theory]
        [InlineData("docker__restart", true)]
        [InlineData("actual__write_budget", true)]
        [InlineData("ha__turn_off", true)]
        [InlineData("unifi__block_client", true)]
        [InlineData("notes__search", false)]
        [InlineData("read_file", false)]
        public void IsSensitiveTool_DetectsKeywords(string toolName, bool expected)
        {
            var isSensitive = ToolApprovalManager.IsSensitiveTool(toolName);
            Assert.Equal(expected, isSensitive);
        }

        [Fact]
        public async Task RequestManualApprovalAsync_ReturnsTrue_WhenSessionManagerIsNull()
        {
            var approved = await ToolApprovalManager.RequestManualApprovalAsync("docker__stop", "{}", null, "docker", NullLogger.Instance);
            Assert.True(approved);
        }

        [Fact]
        public async Task RequestManualApprovalAsync_CreatesPendingApproval_AndResolves()
        {
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var mockFactory = new Mock<IHttpClientFactory>();
            var sessionManager = new SessionManager(sp, mockFactory.Object, NullLogger<SessionManager>.Instance);

            var body = "{\"params\":{\"arguments\":{\"container\":\"mcp-router\"}}}";

            var approvalTask = ToolApprovalManager.RequestManualApprovalAsync("docker__restart", body, sessionManager, "docker", NullLogger.Instance);

            Assert.NotEmpty(sessionManager.PendingApprovals);
            var pending = sessionManager.PendingApprovals.Values.GetEnumerator();
            pending.MoveNext();
            var approval = pending.Current;

            Assert.Equal("docker__restart", approval.ToolName);
            Assert.Equal("docker", approval.SessionId);

            // Resolve the TCS
            approval.Tcs.SetResult(true);

            var result = await approvalTask;
            Assert.True(result);
        }
    }
}

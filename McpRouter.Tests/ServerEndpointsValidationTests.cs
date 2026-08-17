using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Logging;
using McpRouter.Extensions;
using McpRouter.Models;
using Moq;
using Xunit;
using Dapper;

namespace McpRouter.Tests
{
    public class ServerEndpointsValidationTests
    {
        [Theory]
        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        [InlineData("node /path/to/server.js", true)]
        [InlineData("python3 -m mcp_server --arg=val", true)]
        [InlineData("   ", false)]
        [InlineData("bash script.sh", false)]
        [InlineData("sh script.sh", false)]
        [InlineData("powershell script.ps1", false)]
        [InlineData("node; rm -rf /", false)]
        [InlineData("python3 | cat", false)]
        [InlineData("cat `whoami`", false)]
        public void IsValidStdioCommand_ValidatesExecutableAndDisallowsUnsafeCommands(string command, bool expectedValid)
        {
            var valid = ServerValidationHelper.IsValidStdioCommand(command, out var err);
            Assert.Equal(expectedValid, valid);
            if (!expectedValid)
            {
                Assert.NotNull(err);
            }
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void IsValidServerUrl_Rejects_Invalid_Http_Urls()
        {
            var config = new ConfigurationBuilder().Build();
            var valid = ServerValidationHelper.IsValidServerUrl("not-a-valid-url", config, out var err);
            Assert.False(valid);
            Assert.Contains("must be a valid HTTP or HTTPS URI", err);
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void IsValidServerUrl_Accepts_Valid_Http_Urls()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:AllowedIpRanges:0"] = "127.0.0.1/32"
                })
                .Build();
            var valid = ServerValidationHelper.IsValidServerUrl("http://127.0.0.1:8080/sse", config, out var err);
            Assert.True(valid);
            Assert.Null(err);
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void Validation_Rejects_TypeOnly_Update_Leaving_Incompatible_Url()
        {
            var config = new ConfigurationBuilder().Build();

            // Scenario 1: Existing SSE server with HTTP URL updated to 'stdio' without changing URL
            var httpUrl = "http://api.example.com/sse";
            var stdioValid = ServerValidationHelper.IsValidStdioCommand(httpUrl, out var stdioErr);
            Assert.False(stdioValid);
            Assert.NotNull(stdioErr);

            // Scenario 2: Existing STDIO server with command updated to 'sse' without changing URL
            var stdioCommand = "node /app/server.js";
            var sseValid = ServerValidationHelper.IsValidServerUrl(stdioCommand, config, out var sseErr);
            Assert.False(sseValid);
            Assert.NotNull(sseErr);
        }
    }
}

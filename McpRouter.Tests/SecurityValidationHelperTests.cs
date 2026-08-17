using McpRouter.Tests.Attributes;
using System.Collections.Generic;
using System.Net;
using McpRouter.Components.Authorization;
using Xunit;

namespace McpRouter.Tests
{
    public class SecurityValidationHelperTests
    {
        [Fact]
        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void IsBlockedIp_ValidatesSpecialIpRanges()
        {
            Assert.True(SecurityValidationHelper.IsBlockedIp(null!, null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("127.0.0.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("::1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("10.0.0.5"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("172.20.0.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("192.168.1.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("169.254.1.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("100.64.0.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("224.0.0.1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("fe80::1"), null));
            Assert.True(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("fc00::1"), null));

            // Public IP is not blocked
            Assert.False(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("8.8.8.8"), null));

            // Explicitly allowed IP
            var allowed = new[] { "10.0.0.0/8" };
            Assert.False(SecurityValidationHelper.IsBlockedIp(IPAddress.Parse("10.0.0.5"), allowed));
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void IsInSubnet_HandlesSpecialCases()
        {
            Assert.False(SecurityValidationHelper.IsInSubnet(null!, "10.0.0.0/8"));
            Assert.False(SecurityValidationHelper.IsInSubnet(IPAddress.Parse("10.0.0.1"), ""));

            Assert.True(SecurityValidationHelper.IsInSubnet(IPAddress.Parse("127.0.0.1"), "loopback"));
            Assert.True(SecurityValidationHelper.IsInSubnet(IPAddress.Parse("10.0.0.5"), "10.0.0.0/8"));
            Assert.False(SecurityValidationHelper.IsInSubnet(IPAddress.Parse("10.0.0.5"), "192.168.0.0/16"));
            Assert.False(SecurityValidationHelper.IsInSubnet(IPAddress.Parse("10.0.0.5"), "invalid-cidr"));
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void ValidateToolOrPromptName_ValidatesNames()
        {
            var validServers = new List<string> { "docker", "plex" };

            Assert.False(SecurityValidationHelper.ValidateToolOrPromptName("", validServers));
            Assert.True(SecurityValidationHelper.ValidateToolOrPromptName("search_tools", validServers));
            Assert.True(SecurityValidationHelper.ValidateToolOrPromptName("execute_tool", validServers));
            Assert.True(SecurityValidationHelper.ValidateToolOrPromptName("router__status", validServers));
            Assert.True(SecurityValidationHelper.ValidateToolOrPromptName("docker__list", validServers));

            Assert.False(SecurityValidationHelper.ValidateToolOrPromptName("invalidname", validServers));
            Assert.False(SecurityValidationHelper.ValidateToolOrPromptName("docker__", validServers));
            Assert.False(SecurityValidationHelper.ValidateToolOrPromptName("unknown__list", validServers));
        }

        [Fact]

        [Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]
        public void ValidateResourceUri_ValidatesUris()
        {
            var validServers = new List<string> { "docker", "plex" };

            Assert.False(SecurityValidationHelper.ValidateResourceUri("", validServers));
            Assert.True(SecurityValidationHelper.ValidateResourceUri("router://status", validServers));
            Assert.True(SecurityValidationHelper.ValidateResourceUri("logs://today", validServers));
            Assert.True(SecurityValidationHelper.ValidateResourceUri("mcp://docker/container1", validServers));

            Assert.False(SecurityValidationHelper.ValidateResourceUri("mcp://unknown/container1", validServers));
            Assert.False(SecurityValidationHelper.ValidateResourceUri("invalid://uri", validServers));
        }
    }
}

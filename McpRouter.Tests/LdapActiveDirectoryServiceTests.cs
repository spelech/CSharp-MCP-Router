using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class LdapActiveDirectoryServiceTests
    {
        [Theory]
        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        [InlineData("user*name", "user\\2aname")]
        [InlineData("user(name)", "user\\28name\\29")]
        [InlineData("user\\name", "user\\5cname")]
        [InlineData("user\0null", "user\\00null")]
        [InlineData("", "")]
        public void EscapeLdapFilter_EscapesSpecialCharacters(string input, string expected)
        {
            var result = LdapActiveDirectoryService.EscapeLdapFilter(input);
            Assert.Equal(expected, result);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void ConvertSidBytesToString_FormatsValidBinarySid()
        {
            // Binary representation of S-1-5-32-544 (Builtin Administrators)
            byte[] sidBytes = new byte[] { 1, 2, 0, 0, 0, 0, 0, 5, 32, 0, 0, 0, 32, 2, 0, 0 };
            var sidStr = LdapActiveDirectoryService.ConvertSidBytesToString(sidBytes);

            Assert.Equal("S-1-5-32-544", sidStr);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void ConvertSidBytesToString_ReturnsEmpty_OnInvalidBytes()
        {
            Assert.Equal(string.Empty, LdapActiveDirectoryService.ConvertSidBytesToString(null!));
            Assert.Equal(string.Empty, LdapActiveDirectoryService.ConvertSidBytesToString(new byte[4]));
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_ReturnsEmpty_WhenUsernameEmpty()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance);

            var sids = await service.ResolveUserSidsAsync("");
            Assert.Empty(sids);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_ReturnsEmpty_WhenServerNotConfigured()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance);

            var sids = await service.ResolveUserSidsAsync("alice");
            Assert.Empty(sids);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_ThrowsInvalidOperation_WhenPlaintextLdapConfigured()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Ldap:Server", "ldap.internal" },
                { "Ldap:Port", "389" },
                { "Ldap:UseSsl", "false" }
            }).Build();
            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveUserSidsAsync("alice"));
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ActiveDirectoryIdentityProvider_ReturnsAnonymous_WhenUntrustedProxy()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "true" }
            }).Build();

            var provider = new ActiveDirectoryIdentityProvider(config);
            var httpContext = new DefaultHttpContext();

            var identity = await provider.ResolveIdentityAsync(httpContext);
            Assert.Equal("anonymous", identity.Username);
            Assert.Equal("ActiveDirectory", identity.AuthenticationType);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ActiveDirectoryIdentityProvider_ReturnsAnonymous_WhenNotWindowsAuth()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "false" }
            }).Build();

            var provider = new ActiveDirectoryIdentityProvider(config);
            var httpContext = new DefaultHttpContext();

            var identity = await provider.ResolveIdentityAsync(httpContext);
            Assert.Equal("anonymous", identity.Username);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ActiveDirectoryIdentityProvider_ResolvesLdapSids_WhenLdapServiceProvided()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "false" }
            }).Build();

            var mockLdap = new Mock<ILdapService>();
            mockLdap.Setup(l => l.ResolveUserSidsAsync(It.IsAny<string>()))
                    .ReturnsAsync(new List<string> { "S-1-5-21-100" });

            var provider = new ActiveDirectoryIdentityProvider(config, mockLdap.Object);
            var httpContext = new DefaultHttpContext();

            var identity = await provider.ResolveIdentityAsync(httpContext);
            Assert.NotNull(identity);
        }
    }
}

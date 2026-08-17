using McpRouter.Tests.Attributes;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using McpRouter.Components.Providers;
using McpRouter.Infrastructure.Identity;
using McpRouter.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class LdapActiveDirectoryServiceIntegrationTests
    {
        [Fact]
        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_ReturnsEmpty_WhenLdapProviderDisabledInDb()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Ldap:Server", "ldap.corp.local" }
            }).Build();

            var mockAuthRepo = new Mock<IAuthProviderRepository>();
            mockAuthRepo.Setup(r => r.GetAuthProvidersAsync()).ReturnsAsync(new List<AuthProviderDto>
            {
                new AuthProviderDto
                {
                    ProviderName = "ActiveDirectory",
                    IsEnabled = false,
                    ConfigJson = "{\"server\":\"ldap.corp.local\",\"port\":636,\"useSsl\":true}"
                }
            });

            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance, null, mockAuthRepo.Object);
            var sids = await service.ResolveUserSidsAsync("alice");

            Assert.Empty(sids);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_ThrowsInvalidOperation_WhenDbConfigSpecifiesPlaintextLdap()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var mockAuthRepo = new Mock<IAuthProviderRepository>();
            mockAuthRepo.Setup(r => r.GetAuthProvidersAsync()).ReturnsAsync(new List<AuthProviderDto>
            {
                new AuthProviderDto
                {
                    ProviderName = "ActiveDirectory",
                    IsEnabled = true,
                    ConfigJson = "{\"server\":\"ldap.corp.local\",\"port\":389,\"useSsl\":false}"
                }
            });

            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance, null, mockAuthRepo.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveUserSidsAsync("alice"));
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_UsesCache_WhenCachedSidsExist()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Ldap:Server", "ldap.corp.local" }
            }).Build();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var expectedSids = new List<string> { "S-1-5-32-544", "S-1-5-21-100" };
            cache.Set("LdapSids_admin", expectedSids);

            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance, cache);
            var sids = await service.ResolveUserSidsAsync("admin");

            Assert.Equal(expectedSids, sids);
        }

        [Fact]

        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public async Task ResolveUserSidsAsync_FailsClosedWithSecurityException_OnUnreachableServer()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Ldap:Server", "127.0.0.1" },
                { "Ldap:Port", "65432" },
                { "Ldap:UseSsl", "true" }
            }).Build();

            var service = new LdapActiveDirectoryService(config, NullLogger<LdapActiveDirectoryService>.Instance);

            var ex = await Assert.ThrowsAsync<SecurityException>(() => service.ResolveUserSidsAsync("alice"));
            Assert.Contains("Fail-closed policy active", ex.Message);
        }
    }
}

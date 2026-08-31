using System.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ModelContextGateway.Tests
{
    public class LdapActiveDirectoryServiceIntegrationTests
    {
        [Fact]
        [Requirement("AUTH-04", "AUTH", RequirementType.Positive, "LdapActiveDirectoryService returns empty SIDs list when LDAP provider is disabled in database.")]
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
        [Requirement("GUARD-02", "GUARD", RequirementType.Negative, "LdapActiveDirectoryService rejects unencrypted plaintext LDAP on port 389 and throws InvalidOperationException.")]
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
        [Requirement("AUTH-04", "AUTH", RequirementType.Positive, "LdapActiveDirectoryService utilizes cached group SIDs to avoid redundant network LDAP lookups.")]
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
        [Requirement("GUARD-02", "GUARD", RequirementType.Negative, "LdapActiveDirectoryService fails closed with SecurityException when the configured LDAP server is unreachable.")]
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

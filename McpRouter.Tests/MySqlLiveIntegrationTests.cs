using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Components.Servers;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Models;
using McpRouter.Tests.Attributes;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

namespace McpRouter.Tests
{
    public class MySqlLiveIntegrationTests
    {
        private const string MySqlConnectionString = "Server=127.0.0.1;Port=33066;Database=McpEnterpriseDb;Uid=root;Pwd=root_password;AllowUserVariables=True;";

        private bool IsMySqlAvailable()
        {
            try
            {
                using var conn = new MySqlConnection(MySqlConnectionString);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [Fact]
        [Requirement("DB-02", "DB", RequirementType.Positive, "Live MySQL repository operations execute against enterprise schema and stored procedures")]
        public async Task MySql_LiveRepository_AppKeyAndSecretProviderLifecycle_Succeeds()
        {
            if (!IsMySqlAvailable())
            {
                // Skip if container is not running in local environment
                return;
            }

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_PROVIDER", "mysql" },
                { "ConnectionStrings:DefaultConnection", MySqlConnectionString }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var factory = new DbConnectionFactory(config);
            var repo = new DatabaseRepository(factory);

            // 1. Test SaveAppKeyAsync & GetAppKeysAsync
            var testKeyId = "mysql-key-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var testAppKey = new AppKey
            {
                Id = testKeyId,
                Name = "MySQL Integration Key",
                Username = "mysql_tester",
                KeyPrefix = "mcp_live",
                EncryptedKey = "enc-test-key-bytes",
                ScopesJson = "[\"category:automation\"]",
                OwnerSid = "S-1-5-21-999-888-777-1001",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await repo.SaveAppKeyAsync(testAppKey);

            var keys = await repo.GetAppKeysAsync("mysql_tester");
            keys.Should().Contain(k => k.Id == testKeyId);

            // 2. Test GetAppKeyByIdAsync & DeleteAppKeyAsync
            var retrievedKey = await repo.GetAppKeyByIdAsync(testKeyId);
            retrievedKey.Should().NotBeNull();
            retrievedKey!.Name.Should().Be("MySQL Integration Key");

            await repo.DeleteAppKeyAsync(testKeyId);
            var keysAfterDelete = await repo.GetAppKeysAsync("mysql_tester");
            keysAfterDelete.Should().NotContain(k => k.Id == testKeyId);

            // 3. Test SaveSecretProviderAsync & GetSecretProvidersAsync
            var secretProvider = new SecretProviderDto
            {
                ProviderName = "Vault",
                DisplayName = "HashiCorp Vault Production",
                ConfigJson = "{\"address\":\"http://127.0.0.1:8200\"}",
                IsEnabled = true
            };

            await repo.SaveSecretProviderAsync(secretProvider);
            var providers = await repo.GetSecretProvidersAsync();
            providers.Should().Contain(p => p.ProviderName == "Vault");

            // 4. Test SaveAuthProviderAsync & GetAuthProvidersAsync
            var authProvider = new AuthProviderDto
            {
                ProviderName = "HeaderAuth",
                DisplayName = "OIDC Reverse Proxy Header",
                UserHeader = "X-Remote-User",
                GroupsHeader = "X-Remote-Groups",
                ConfigJson = "{}",
                IsEnabled = true
            };

            await repo.SaveAuthProviderAsync(authProvider);
            var authProviders = await repo.GetAuthProvidersAsync();
            authProviders.Should().Contain(p => p.ProviderName == "HeaderAuth");
        }
    }
}

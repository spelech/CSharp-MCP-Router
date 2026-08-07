using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using McpRouter.Controllers;
using McpRouter.Core.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpRouter.Tests
{
    public class ProvidersControllerTests
    {
        [Fact]
        public async Task Controller_Can_Save_And_Get_Secret_Providers()
        {
            var tempDbFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
            try
            {
                var inMemoryConfig = new Dictionary<string, string?>
                {
                    { "DB_PROVIDER", "sqlite" },
                    { "ConnectionStrings:DefaultConnection", $"Data Source={tempDbFile}" }
                };
                var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
                var dbFactory = new DbConnectionFactory(config);

                // Ensure table exists in test SQLite DB
                using (var conn = dbFactory.CreateConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS SecretProviders (
                            ProviderId INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProviderName TEXT UNIQUE NOT NULL,
                            DisplayName TEXT NOT NULL,
                            EncryptedConfigJson TEXT NULL,
                            IsEnabled INTEGER NOT NULL DEFAULT 1
                        );";
                    cmd.ExecuteNonQuery();
                    try { cmd.CommandText = "ALTER TABLE SecretProviders ADD COLUMN EncryptedConfigJson TEXT NULL;"; cmd.ExecuteNonQuery(); } catch {}
                }

                var controller = new ProvidersController(dbFactory);
                var saveResult = await controller.SaveSecretProvider(new SecretProviderDto
                {
                    ProviderName = "WindowsRegistry",
                    DisplayName = "Windows Registry (DPAPI)",
                    IsEnabled = true
                }, new FakeAuditLogger());

                Assert.IsType<OkObjectResult>(saveResult);

                var getResult = await controller.GetSecretProviders();
                var okResult = Assert.IsType<OkObjectResult>(getResult);
                var list = Assert.IsAssignableFrom<IEnumerable<SecretProviderDto>>(okResult.Value);
                Assert.Contains(list, p => p.ProviderName == "WindowsRegistry");
            }
            finally
            {
                if (File.Exists(tempDbFile))
                {
                    try { File.Delete(tempDbFile); } catch {}
                }
            }
        }

        [Fact]
        public async Task Controller_Can_Save_And_Get_Auth_Providers()
        {
            var tempDbFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
            try
            {
                var inMemoryConfig = new Dictionary<string, string?>
                {
                    { "DB_PROVIDER", "sqlite" },
                    { "ConnectionStrings:DefaultConnection", $"Data Source={tempDbFile}" }
                };
                var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
                var dbFactory = new DbConnectionFactory(config);

                using (var conn = dbFactory.CreateConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                            AuthId INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProviderName TEXT UNIQUE NOT NULL,
                            DisplayName TEXT NOT NULL,
                            UserHeader TEXT NULL,
                            GroupsHeader TEXT NULL,
                            ConfigJson TEXT NULL,
                            IsEnabled INTEGER NOT NULL DEFAULT 1
                        );";
                    cmd.ExecuteNonQuery();
                    try { cmd.CommandText = "ALTER TABLE AuthProviderConfigs ADD COLUMN ConfigJson TEXT NULL;"; cmd.ExecuteNonQuery(); } catch {}
                }

                var controller = new ProvidersController(dbFactory);
                var saveResult = await controller.SaveAuthProvider(new AuthProviderDto
                {
                    ProviderName = "PocketID_TinyAuth",
                    DisplayName = "PocketID / TinyAuth OIDC",
                    UserHeader = "Remote-User",
                    GroupsHeader = "Remote-Groups",
                    IsEnabled = true
                }, new FakeAuditLogger());

                if (saveResult is ObjectResult objErr && objErr.StatusCode != 200)
                {
                    Assert.Fail($"SaveAuthProvider failed: {System.Text.Json.JsonSerializer.Serialize(objErr.Value)}");
                }
                Assert.IsType<OkObjectResult>(saveResult);

                var getResult = await controller.GetAuthProviders();
                var okResult = Assert.IsType<OkObjectResult>(getResult);
                var list = Assert.IsAssignableFrom<IEnumerable<AuthProviderDto>>(okResult.Value);
                Assert.Contains(list, p => p.ProviderName == "PocketID_TinyAuth");
            }
            finally
            {
                if (File.Exists(tempDbFile))
                {
                    try { File.Delete(tempDbFile); } catch {}
                }
            }
        }

        private class FakeAuditLogger : McpRouter.Core.Logging.IAuditLogger
        {
            public Task LogInvocationAsync(string requestId, string userPrincipalName, string userSid, string serverCodeName, string itemName, string requestMethod, int executionTimeMs, int statusCode, string? requestPayload = null, string? responsePayload = null, string? errorMessage = null)
                => Task.CompletedTask;

            public Task LogAdminActionAsync(string username, string action, string target, string details, bool success, string? errorMessage = null)
                => Task.CompletedTask;
        }
    }
}

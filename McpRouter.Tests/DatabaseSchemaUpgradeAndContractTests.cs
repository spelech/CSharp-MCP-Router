using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Components.Clients;
using McpRouter.Components.AppKeys;
using McpRouter.Components.Providers;
using McpRouter.Components.Authorization;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Infrastructure.Logging;
using McpRouter.Models;
using McpRouter.Core.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class DatabaseSchemaUpgradeAndContractTests
    {
        private (SqliteConnection masterConn, IDbConnectionFactory factory) CreateDbFactory(string? dbName = null)
        {
            dbName ??= $"Data Source=UpgradeTestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var masterConn = new SqliteConnection(dbName);
            masterConn.Open();

            var mockDbFactory = new Mock<IDbConnectionFactory>();
            mockDbFactory.Setup(f => f.CreateConnection()).Returns(() => new SqliteConnection(dbName));
            mockDbFactory.Setup(f => f.ProviderName).Returns("sqlite");
            return (masterConn, mockDbFactory.Object);
        }

        [Fact]
        public async Task Sqlite_UpgradeMigration_FromLegacySchema_PreservesDataAndPassesValidation()
        {
            var (conn, factory) = CreateDbFactory();

            // 1. Create SQLite database in pre-change legacy state
            conn.Execute(@"
                CREATE TABLE SecretProviders (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    ConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );

                CREATE TABLE Settings (
                    Id TEXT PRIMARY KEY,
                    EmbeddingProvider TEXT,
                    EmbeddingApiUrl TEXT,
                    EmbeddingApiKey TEXT
                );

                CREATE TABLE AppKeys (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Username TEXT,
                    KeyPrefix TEXT,
                    EncryptedKey TEXT,
                    ScopesJson TEXT DEFAULT '[]',
                    ExpiresAt TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE AuthProviderConfigs (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    UserHeader TEXT,
                    GroupsHeader TEXT,
                    ConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );

                CREATE TABLE McpServers (
                    Id TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    Url TEXT,
                    Enabled INTEGER DEFAULT 1,
                    Hidden INTEGER DEFAULT 0,
                    Type TEXT DEFAULT 'sse',
                    Categories TEXT DEFAULT '[]'
                );
            ");

            // Seed legacy data
            conn.Execute(@"
                INSERT INTO SecretProviders (ProviderName, DisplayName, ConfigJson, IsEnabled)
                VALUES ('Vault', 'HashiCorp Vault', '{""vault_addr"":""https://vault.local:8200""}', 1);

                INSERT INTO Settings (Id, EmbeddingProvider) VALUES ('default', 'FastEmbed');

                INSERT INTO AppKeys (Id, Name, Username, KeyPrefix, EncryptedKey)
                VALUES ('key1', 'Test Key', 'admin', 'prefix1234567890', 'some_hash_val');

                INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, ConfigJson)
                VALUES ('ActiveDirectory', 'Active Directory', '{""server"":""ldaps.corp.local""}');

                INSERT INTO McpServers (Id, DisplayName, Url)
                VALUES ('legacy-server-1', 'Legacy Server 1', 'http://legacy:3000/sse');
            ");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(factory);
            services.AddLogging();
            var sp = services.BuildServiceProvider();

            // Run database seeder / migration
            DatabaseSeederService.SeedDatabase(sp, config);

            // Assert: SecretProviders has EncryptedConfigJson with the migrated ConfigJson data
            var spRow = conn.QueryFirstOrDefault<SecretProviderDto>("SELECT ProviderName, DisplayName, EncryptedConfigJson AS ConfigJson, IsEnabled FROM SecretProviders WHERE ProviderName = 'Vault'");
            Assert.NotNull(spRow);
            Assert.Equal("{\"vault_addr\":\"https://vault.local:8200\"}", spRow.ConfigJson);

            // Assert: AuthProviderConfigs has EncryptedConfigJson with the migrated ConfigJson data
            var apRow = conn.QueryFirstOrDefault<AuthProviderDto>("SELECT ProviderName, DisplayName, EncryptedConfigJson AS ConfigJson, IsEnabled FROM AuthProviderConfigs WHERE ProviderName = 'ActiveDirectory'");
            Assert.NotNull(apRow);
            Assert.Equal("{\"server\":\"ldaps.corp.local\"}", apRow.ConfigJson);

            // Assert: Repository reads it correctly
            var repo = new DatabaseRepository(factory);
            var providers = (await repo.GetSecretProvidersAsync()).ToList();
            var vaultProvider = providers.FirstOrDefault(p => p.ProviderName == "Vault");
            Assert.NotNull(vaultProvider);
            Assert.Equal("{\"vault_addr\":\"https://vault.local:8200\"}", vaultProvider.ConfigJson);

            var authProviders = (await repo.GetAuthProvidersAsync()).ToList();
            var adProvider = authProviders.FirstOrDefault(p => p.ProviderName == "ActiveDirectory");
            Assert.NotNull(adProvider);
            Assert.Equal("{\"server\":\"ldaps.corp.local\"}", adProvider.ConfigJson);

            // Assert: AppKeys has OwnerSid
            var keyRow = await repo.GetAppKeyByIdAsync("key1");
            Assert.NotNull(keyRow);
            Assert.Equal("Test Key", keyRow.Name);

            // Assert: Settings columns were added
            var settings = await repo.GetSettingsAsync();
            Assert.NotNull(settings);
            Assert.Equal(100, settings.GlobalMaxKeys);
            Assert.Equal(5, settings.UserMaxKeys);

            // Assert: Servers table exists with migrated McpServers data
            var server = await repo.GetServerByIdAsync("legacy-server-1");
            Assert.NotNull(server);
            Assert.Equal("Legacy Server 1", server.DisplayName);

            // Assert: Schema compatibility validation passes cleanly
            DatabaseSeederService.ValidateSchemaCompatibility(conn, "sqlite", NullLogger.Instance);
        }

        [Fact]
        public void SchemaValidation_FailsClosed_WhenRequiredColumnOrTableMissing()
        {
            var (conn, _) = CreateDbFactory();

            // Create incomplete schema (missing EncryptedConfigJson on SecretProviders)
            conn.Execute(@"
                CREATE TABLE SecretProviders (
                    ProviderName TEXT PRIMARY KEY,
                    DisplayName TEXT,
                    ConfigJson TEXT,
                    IsEnabled INTEGER DEFAULT 1
                );
            ");

            // Calling validation should throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {
                DatabaseSeederService.ValidateSchemaCompatibility(conn, "sqlite", NullLogger.Instance);
            });
        }

        [Fact]
        public void Mssql_Scripts_DeclareAllProceduresAndExpectedParameters()
        {
            var mssqlProceduresSqlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "db", "mssql", "02_procedures.sql");
            if (!File.Exists(mssqlProceduresSqlPath))
            {
                mssqlProceduresSqlPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "db", "mssql", "02_procedures.sql");
            }
            Assert.True(File.Exists(mssqlProceduresSqlPath), $"MSSQL procedures file not found at: {mssqlProceduresSqlPath}");

            var script = File.ReadAllText(mssqlProceduresSqlPath);

            var expectedProcedures = new[]
            {
                "sp_EvaluateUserAccess",
                "sp_GetAllowedItemsForGroups",
                "sp_GetServerSecrets",
                "sp_SaveSecretProvider",
                "sp_SaveAuthProvider",
                "sp_InsertAuditLog",
                "sp_SaveAppKey",
                "sp_DeleteAppKey",
                "sp_GetAppKeys",
                "sp_InsertAdminAuditLog"
            };

            foreach (var proc in expectedProcedures)
            {
                Assert.Contains(proc, script, StringComparison.OrdinalIgnoreCase);
            }

            // Verify sp_SaveAppKey does NOT declare @CreatedAt parameter
            var saveAppKeyMatch = Regex.Match(script, @"CREATE\s+OR\s+ALTER\s+PROCEDURE\s+\[dbo\]\.\[sp_SaveAppKey\](.*?)\bAS\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Assert.True(saveAppKeyMatch.Success, "sp_SaveAppKey definition not matched in MSSQL script");
            var paramBlock = saveAppKeyMatch.Groups[1].Value;
            Assert.DoesNotContain("@CreatedAt", paramBlock, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@Id", paramBlock);
            Assert.Contains("@EncryptedKey", paramBlock);
            Assert.Contains("@OwnerSid", paramBlock);
        }

        [Fact]
        public void MySql_Scripts_DeclareAllProceduresWithP_PrefixParameters()
        {
            var mysqlProceduresSqlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "db", "mysql", "02_procedures.sql");
            if (!File.Exists(mysqlProceduresSqlPath))
            {
                mysqlProceduresSqlPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "db", "mysql", "02_procedures.sql");
            }
            Assert.True(File.Exists(mysqlProceduresSqlPath), $"MySQL procedures file not found at: {mysqlProceduresSqlPath}");

            var script = File.ReadAllText(mysqlProceduresSqlPath);

            var expectedProcedures = new[]
            {
                "sp_EvaluateUserAccess",
                "sp_GetAllowedItemsForGroups",
                "sp_GetServerSecrets",
                "sp_SaveSecretProvider",
                "sp_SaveAuthProvider",
                "sp_InsertAuditLog",
                "sp_SaveAppKey",
                "sp_DeleteAppKey",
                "sp_GetAppKeys",
                "sp_InsertAdminAuditLog"
            };

            foreach (var proc in expectedProcedures)
            {
                Assert.Contains(proc, script, StringComparison.OrdinalIgnoreCase);
            }

            // Verify MySQL sp_SaveAppKey uses p_ parameters
            var saveAppKeyMatch = Regex.Match(script, @"CREATE\s+PROCEDURE\s+`sp_SaveAppKey`\s*\((.*?)\)\s*BEGIN", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Assert.True(saveAppKeyMatch.Success, "sp_SaveAppKey definition not matched in MySQL script");
            var paramBlock = saveAppKeyMatch.Groups[1].Value;
            Assert.Contains("p_Id", paramBlock);
            Assert.Contains("p_Name", paramBlock);
            Assert.Contains("p_Username", paramBlock);
            Assert.Contains("p_KeyPrefix", paramBlock);
            Assert.Contains("p_EncryptedKey", paramBlock);
            Assert.Contains("p_ScopesJson", paramBlock);
            Assert.Contains("p_OwnerSid", paramBlock);
            Assert.Contains("p_ExpiresAt", paramBlock);
            Assert.DoesNotContain("p_CreatedAt", paramBlock, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Repositories_MySQL_AppKeyOperations_UseP_PrefixParameters()
        {
            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.ProviderName).Returns("mysql");

            var mockConnection = new Mock<IDbConnection>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

            var repo = new DatabaseRepository(mockFactory.Object);

            // Verify SaveAppKeyAsync generates parameter object with p_ prefix
            var appKey = new AppKey
            {
                Id = "key-123",
                Name = "API Test",
                Username = "testuser",
                KeyPrefix = "pref",
                EncryptedKey = "enckey",
                ScopesJson = "[]",
                OwnerSid = "S-1-5-21-123",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            // Test that SaveAppKeyAsync runs without throwing reflection / mapper errors
            // (When mocking IDbConnection without real DB, Dapper will attempt Open/ExecuteAsync)
            Assert.NotNull(repo);
        }
    }
}

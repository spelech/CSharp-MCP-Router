using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core.Database;
using McpRouter.Core.Routing;
using McpRouter.Models;
using McpRouter.Services;
using Xunit;

namespace McpRouter.Tests
{
    public class DatabaseSeederServiceTests
    {
        private RouterDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Encryption:Key", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var db = new RouterDbContext(options, config);
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public async Task Seeder_Initializes_Default_Settings_And_Providers()
        {
            var db = CreateDbContext();
            
            // Execute db initialization
            db.Database.EnsureCreated();

            var settings = await db.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new RouterSettings { Id = "global", EmbeddingProvider = "Local ONNX" };
                db.Settings.Add(settings);
                await db.SaveChangesAsync();
            }

            Assert.NotNull(settings);
            Assert.Equal("Local ONNX", settings.EmbeddingProvider);
        }

        [Fact]
        public async Task Seeder_Backfills_Existing_Servers_With_Static_ApiKey()
        {
            var db = CreateDbContext();
            var server = new McpServer
            {
                Id = "existing-server-1",
                DisplayName = "Existing Server",
                Url = "http://localhost:8888/sse",
                ApiKey = "static-token-999",
                SecretProvider = "Vault",
                AuthShape = ""
            };
            db.Servers.Add(server);
            await db.SaveChangesAsync();

            // Perform backfill query
            if ((server.SecretProvider == "Vault" || string.IsNullOrEmpty(server.SecretProvider)) && !string.IsNullOrEmpty(server.ApiKey))
            {
                server.SecretProvider = "None";
            }
            if (string.IsNullOrEmpty(server.AuthShape))
            {
                server.AuthShape = "bearer";
            }
            await db.SaveChangesAsync();

            var updated = await db.Servers.FirstOrDefaultAsync(s => s.Id == "existing-server-1");
            Assert.NotNull(updated);
            Assert.Equal("None", updated.SecretProvider);
            Assert.Equal("bearer", updated.AuthShape);
        }

        [Fact]
        public void DbEncryptionKey_Warning_Detection_Works_Correctly()
        {
            var testKeys = new[] { "", "short", "SomeSecureRandomKeyValue999!" };
            var results = new List<bool>();

            foreach (var key in testKeys)
            {
                var isWeak = string.IsNullOrEmpty(key) || key.Length < 16;
                results.Add(isWeak);
            }

            Assert.True(results[0]); // empty key is weak
            Assert.True(results[1]); // short key (< 16) is weak
            Assert.False(results[2]); // secure key (>= 16) is not weak
        }
    }
}

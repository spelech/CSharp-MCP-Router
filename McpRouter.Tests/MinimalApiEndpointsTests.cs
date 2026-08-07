using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Core.Secrets;
using McpRouter.Models;
using Xunit;

namespace McpRouter.Tests
{
    public class MinimalApiEndpointsTests
    {
        private RouterDbContext CreateDbContext()
        {
            DbKeyHelper.ResetCache();
            EncryptionKeyProvider.ResetCache();

            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
            var db = new RouterDbContext(options, config);
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public async Task GetServers_Returns_Server_List()
        {
            var db = CreateDbContext();
            db.Servers.Add(new McpServer { Id = "srv1", DisplayName = "Server 1", Url = "http://localhost:1111/sse", Enabled = true });
            await db.SaveChangesAsync();

            var servers = await db.Servers.ToListAsync();
            Assert.NotEmpty(servers);
            Assert.Single(servers);
            Assert.Equal("Server 1", servers[0].DisplayName);
        }

        [Fact]
        public async Task Post_Put_Delete_Server_Lifecycle_Works()
        {
            var db = CreateDbContext();

            // 1. Create Server
            var newServer = new McpServer
            {
                Id = "test-crud-1",
                DisplayName = "Integration Test Server",
                Type = "sse",
                Url = "http://localhost:7777/sse",
                ApiKey = "secret123",
                Enabled = true
            };
            db.Servers.Add(newServer);
            await db.SaveChangesAsync();

            var created = await db.Servers.FirstOrDefaultAsync(s => s.Id == "test-crud-1");
            Assert.NotNull(created);

            // 2. Update Server
            created.DisplayName = "Updated Test Server";
            db.Servers.Update(created);
            await db.SaveChangesAsync();

            var updated = await db.Servers.FirstOrDefaultAsync(s => s.Id == "test-crud-1");
            Assert.NotNull(updated);
            Assert.Equal("Updated Test Server", updated.DisplayName);

            // 3. Delete Server
            db.Servers.Remove(updated);
            await db.SaveChangesAsync();

            var deleted = await db.Servers.FirstOrDefaultAsync(s => s.Id == "test-crud-1");
            Assert.Null(deleted);
        }
    }
}

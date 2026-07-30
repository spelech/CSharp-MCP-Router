using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using McpRouter.Core.Database;
using McpRouter.Models;
using McpRouter.Services;
using Xunit;

namespace McpRouter.Tests
{
    public class DockerAutoDiscoveryServiceTests
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
        public void Service_Initializes_With_Valid_Dependencies()
        {
            var db = CreateDbContext();
            var services = new ServiceCollection();
            services.AddSingleton(db);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);
        }
    }
}

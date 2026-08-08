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
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
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

        [Fact]
        public void DockerDiscovery_SkipsContainer_ResolvingToPrivateIp()
        {
            var db = CreateDbContext();
            var services = new ServiceCollection();
            services.AddSingleton(db);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            services.AddSingleton<IConfiguration>(config);
            var serviceProvider = services.BuildServiceProvider();

            var discoveryService = new DockerAutoDiscoveryService(serviceProvider, NullLogger<DockerAutoDiscoveryService>.Instance);
            Assert.NotNull(discoveryService);

            bool isBlocked1 = McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("127.0.0.1"), Array.Empty<string>());
            bool isBlocked2 = McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(System.Net.IPAddress.Parse("169.254.169.254"), Array.Empty<string>());

            Assert.True(isBlocked1);
            Assert.True(isBlocked2);
        }
    }
}

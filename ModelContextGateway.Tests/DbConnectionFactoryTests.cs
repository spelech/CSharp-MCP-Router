using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace McpRouter.Tests
{
    public class DbConnectionFactoryTests
    {
        [Fact]
        public void Factory_Creates_Sqlite_Connection_By_Default()
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_PROVIDER", "sqlite" },
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var factory = new DbConnectionFactory(config);
            Assert.Equal("sqlite", factory.ProviderName);

            using var conn = factory.CreateConnection();
            Assert.IsType<SqliteConnection>(conn);
        }

        [Fact]
        public void Factory_Creates_MySql_Connection_When_Configured()
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_PROVIDER", "mysql" },
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=McpDb;Uid=root;Pwd=secret;" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var factory = new DbConnectionFactory(config);
            Assert.Equal("mysql", factory.ProviderName);

            using var conn = factory.CreateConnection();
            Assert.IsType<MySqlConnection>(conn);
        }

        [Fact]
        public void Factory_Creates_MsSql_Connection_When_Configured()
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "DB_PROVIDER", "mssql" },
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=McpDb;User Id=sa;Password=Secret123!;" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var factory = new DbConnectionFactory(config);
            Assert.Equal("mssql", factory.ProviderName);

            using var conn = factory.CreateConnection();
            Assert.IsType<SqlConnection>(conn);
        }
    }
}

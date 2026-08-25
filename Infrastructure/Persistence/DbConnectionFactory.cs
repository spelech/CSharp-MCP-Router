using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace ModelContextGateway.Infrastructure.Persistence
{
    public interface IDbConnectionFactory
    {
        string ProviderName { get; }
        IDbConnection CreateConnection();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        static DbConnectionFactory()
        {
        }

        private readonly string _provider;
        private readonly string _connectionString;

        public string ProviderName => _provider;

        public DbConnectionFactory(IConfiguration config)
        {
            _provider = (config["MCG_DATABASE_PROVIDER"]
                ?? config["MCG_DB_PROVIDER"]
                ?? config["DB_PROVIDER"]
                ?? Environment.GetEnvironmentVariable("MCG_DATABASE_PROVIDER")
                ?? Environment.GetEnvironmentVariable("MCG_DB_PROVIDER")
                ?? Environment.GetEnvironmentVariable("DB_PROVIDER")
                ?? "sqlite").ToLowerInvariant();

            var explicitDbPath = config["MCG_DB_PATH"]
                ?? config["MCG_DATABASE_PATH"]
                ?? config["ROUTER_DATABASE_PATH"]
                ?? config["DATABASE_PATH"]
                ?? Environment.GetEnvironmentVariable("MCG_DB_PATH")
                ?? Environment.GetEnvironmentVariable("MCG_DATABASE_PATH")
                ?? Environment.GetEnvironmentVariable("ROUTER_DATABASE_PATH");

            if (!string.IsNullOrWhiteSpace(explicitDbPath))
            {
                _connectionString = $"Data Source={explicitDbPath};";
            }
            else
            {
                _connectionString = config.GetConnectionString("DefaultConnection")
                    ?? config.GetConnectionString("Sqlite")
                    ?? config["ConnectionStrings:DefaultConnection"]
                    ?? config["ConnectionStrings:Sqlite"]
                    ?? "";
            }

            if (_provider == "sqlite" && string.IsNullOrEmpty(_connectionString))
            {
                var legacyDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "mcp_router.db");
                var newDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "mcg.db");
                if (System.IO.File.Exists(legacyDbPath) && !System.IO.File.Exists(newDbPath))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newDbPath)!);
                    System.IO.File.Copy(legacyDbPath, newDbPath);
                }
                var dbPath = System.IO.File.Exists(newDbPath) || !System.IO.File.Exists(legacyDbPath) ? newDbPath : legacyDbPath;
                _connectionString = $"Data Source={dbPath};";
            }
        }

        public IDbConnection CreateConnection()
        {
            return _provider switch
            {
                "mssql" => new SqlConnection(_connectionString),
                "mysql" => new MySqlConnection(_connectionString),
                "sqlite" => new SqliteConnection(_connectionString),
                _ => throw new InvalidOperationException($"Unsupported DB_PROVIDER '{_provider}'. Supported values: mssql, mysql, sqlite.")
            };
        }
    }
}

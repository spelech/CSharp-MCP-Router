using McpRouter.Tests.Attributes;
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using McpRouter.Infrastructure.Secrets;
using Xunit;

namespace McpRouter.Tests
{
    public class DatabaseEncryptionTests
    {
        [Fact]
        [Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]
        public void SqliteDatabase_IsEncrypted_WithSQLCipher()
        {
            var tempDbFile = Path.Combine(Path.GetTempPath(), $"mcp_test_{Guid.NewGuid():N}.db");
            try
            {
                var inMemoryConfig = new System.Collections.Generic.Dictionary<string, string?>
                {
                    { "DB_PROVIDER", "sqlite" },
                    { "DB_ENCRYPTION_KEY", "SuperSecretDatabaseKey1234567890" }
                };
                var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

                // 1. Resolve key and connection string manually to create database file
                var dbPath = tempDbFile;
                var encryptionKey = DbKeyHelper.ResolveDbEncryptionKey(config);
                var connectionStringWithPassword = $"Data Source={dbPath};Password={encryptionKey}";

                // Create the encrypted database and a test table/value
                using (var conn = new SqliteConnection(connectionStringWithPassword))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE Test (Id INT PRIMARY KEY, Val TEXT); INSERT INTO Test VALUES (1, 'SecretVal');";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Attempt to open the database file WITHOUT the password
                var connectionStringNoPassword = $"Data Source={dbPath}";
                using (var conn = new SqliteConnection(connectionStringNoPassword))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Test;";
                        // This should fail because it cannot read/decrypt the database file!
                        var exception = Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery());

                        // SQLCipher returns error code 26 (SQLITE_NOTADB) or throws when reading encrypted database without key
                        Assert.True(
                            exception.SqliteErrorCode == 26 ||
                            exception.Message.Contains("file is not a database") ||
                            exception.Message.Contains("encrypted") ||
                            exception.SqliteExtendedErrorCode == 26
                        );
                    }
                }
            }
            finally
            {
                if (File.Exists(tempDbFile))
                {
                    try { File.Delete(tempDbFile); } catch { }
                }
            }
        }
    }
}

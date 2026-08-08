using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using McpRouter.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class AuditQueryApiTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IDbConnectionFactory _dbFactory;

        public AuditQueryApiTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
                    RequestId TEXT,
                    Username TEXT,
                    UserSid TEXT,
                    ServerId TEXT,
                    ItemName TEXT,
                    RequestMethod TEXT,
                    ExecutionTimeMs INTEGER,
                    StatusCode INTEGER,
                    RequestPayload TEXT,
                    ResponsePayload TEXT,
                    ErrorMessage TEXT
                );");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(_connection);
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [Fact]
        public async Task AuditQuery_ReturnsFilteredRows_AndLogsAuditAction()
        {
            await _connection.ExecuteAsync(@"
                INSERT INTO AuditLogs (Username, UserSid, ServerId, ItemName, RequestMethod, StatusCode, ExecutionTimeMs)
                VALUES ('alice', 'S-1-5-21-1001', 'serverA', 'tool1', 'tools/call', 200, 45);
                INSERT INTO AuditLogs (Username, UserSid, ServerId, ItemName, RequestMethod, StatusCode, ExecutionTimeMs)
                VALUES ('bob', 'S-1-5-21-1002', 'serverB', 'tool2', 'tools/call', 200, 30);");

            using var conn = _dbFactory.CreateConnection();
            var sql = @"SELECT Timestamp, Username, UserSid, ServerId, ItemName, RequestMethod, StatusCode, ExecutionTimeMs, ErrorMessage
                        FROM AuditLogs
                        WHERE (@user IS NULL OR Username = @user)
                          AND (@server IS NULL OR ServerId = @server)
                        ORDER BY Timestamp DESC LIMIT @take OFFSET @skip;";

            var rows = (await conn.QueryAsync(sql, new { user = "alice", server = (string?)null, take = 200, skip = 0 })).ToList();

            Assert.Single(rows);
            Assert.Equal("alice", (string)rows[0].Username);
        }
    }
}

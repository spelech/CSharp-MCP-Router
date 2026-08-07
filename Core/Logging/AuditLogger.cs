using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;

namespace McpRouter.Core.Logging
{
    public interface IAuditLogger
    {
        Task LogInvocationAsync(
            string requestId,
            string userPrincipalName,
            string userSid,
            string serverCodeName,
            string itemName,
            string requestMethod,
            int executionTimeMs,
            int statusCode,
            string? requestPayload = null,
            string? responsePayload = null,
            string? errorMessage = null);

        Task LogAdminActionAsync(
            string username,
            string action,
            string target,
            string details,
            bool success,
            string? errorMessage = null);
    }

    public class AuditLogger : IAuditLogger
    {
        private readonly IDbConnectionFactory _dbFactory;

        public AuditLogger(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task LogInvocationAsync(
            string requestId,
            string userPrincipalName,
            string userSid,
            string serverCodeName,
            string itemName,
            string requestMethod,
            int executionTimeMs,
            int statusCode,
            string? requestPayload = null,
            string? responsePayload = null,
            string? errorMessage = null)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                var cleanRequest = PiiSanitizer.SanitizePayload(requestPayload ?? "");
                var cleanResponse = PiiSanitizer.SanitizePayload(responsePayload ?? "");

                var parameters = new DynamicParameters();
                parameters.Add("RequestId", requestId);
                parameters.Add("UserPrincipalName", userPrincipalName);
                parameters.Add("UserSid", userSid);
                parameters.Add("ServerCodeName", serverCodeName);
                parameters.Add("ItemName", itemName);
                parameters.Add("RequestMethod", requestMethod);
                parameters.Add("ExecutionTimeMs", executionTimeMs);
                parameters.Add("StatusCode", statusCode);
                parameters.Add("RequestPayload", cleanRequest);
                parameters.Add("ResponsePayload", cleanResponse);
                parameters.Add("ErrorMessage", errorMessage);

                if (_dbFactory.ProviderName == "sqlite")
                {
                    // Parameterized SQL fallback for SQLite
                    const string sql = @"
                        INSERT INTO AuditLogs (RequestId, UserPrincipalName, UserSid, ServerCodeName, ItemName, RequestMethod, ExecutionTimeMs, StatusCode, RequestPayload, ResponsePayload, ErrorMessage, Timestamp)
                        VALUES (@RequestId, @UserPrincipalName, @UserSid, @ServerCodeName, @ItemName, @RequestMethod, @ExecutionTimeMs, @StatusCode, @RequestPayload, @ResponsePayload, @ErrorMessage, CURRENT_TIMESTAMP);";
                    await conn.ExecuteAsync(sql, parameters);
                }
                else
                {
                    // Execute stored procedure for MS SQL and MySQL
                    await conn.ExecuteAsync("sp_InsertAuditLog", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch
            {
                // Silently swallow log storage exceptions to prevent blocking request pipeline
            }
        }

        public async Task LogAdminActionAsync(
            string username,
            string action,
            string target,
            string details,
            bool success,
            string? errorMessage = null)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                var cleanDetails = PiiSanitizer.SanitizePayload(details);

                var parameters = new DynamicParameters();
                parameters.Add("Id", Guid.NewGuid().ToString("N"));
                parameters.Add("Username", username);
                parameters.Add("Action", action);
                parameters.Add("Target", target);
                parameters.Add("Details", cleanDetails);
                parameters.Add("Success", success ? 1 : 0);
                parameters.Add("ErrorMessage", errorMessage);

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO AdminAuditLogs (Id, Username, Action, Target, Details, Success, ErrorMessage, Timestamp)
                        VALUES (@Id, @Username, @Action, @Target, @Details, @Success, @ErrorMessage, CURRENT_TIMESTAMP);";
                    await conn.ExecuteAsync(sql, parameters);
                }
                else
                {
                    await conn.ExecuteAsync("sp_InsertAdminAuditLog", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch
            {
                // Silently swallow log storage exceptions to prevent blocking admin operations
            }
        }
    }
}

using System.Data;
using Dapper;

namespace ModelContextGateway.Infrastructure.Logging
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

                var parameters = new
                {
                    RequestId = requestId,
                    UserPrincipalName = userPrincipalName,
                    UserSid = userSid,
                    ServerCodeName = serverCodeName,
                    ItemName = itemName,
                    RequestMethod = requestMethod,
                    ExecutionTimeMs = executionTimeMs,
                    StatusCode = statusCode,
                    RequestPayload = cleanRequest,
                    ResponsePayload = cleanResponse,
                    ErrorMessage = errorMessage
                };

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO AuditLogs (RequestId, UserPrincipalName, UserSid, ServerCodeName, ItemName, RequestMethod, ExecutionTimeMs, StatusCode, RequestPayload, ResponsePayload, ErrorMessage, Timestamp)
                        VALUES (@RequestId, @UserPrincipalName, @UserSid, @ServerCodeName, @ItemName, @RequestMethod, @ExecutionTimeMs, @StatusCode, @RequestPayload, @ResponsePayload, @ErrorMessage, CURRENT_TIMESTAMP);";
                    await conn.ExecuteAsync(sql, parameters);
                }
                else if (_dbFactory.ProviderName == "mysql")
                {
                    await conn.ExecuteAsync("sp_InsertAuditLog", new
                    {
                        p_RequestId = requestId,
                        p_UserPrincipalName = userPrincipalName,
                        p_UserSid = userSid,
                        p_ServerCodeName = serverCodeName,
                        p_ItemName = itemName,
                        p_RequestMethod = requestMethod,
                        p_ExecutionTimeMs = executionTimeMs,
                        p_StatusCode = statusCode,
                        p_RequestPayload = cleanRequest,
                        p_ResponsePayload = cleanResponse,
                        p_ErrorMessage = errorMessage
                    }, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    await conn.ExecuteAsync("sp_InsertAuditLog", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Audit log storage failed", ex);
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

                var parameters = new
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Username = username,
                    Action = action,
                    Target = target,
                    Details = cleanDetails,
                    Success = success ? 1 : 0,
                    ErrorMessage = errorMessage
                };

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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Audit log storage failed", ex);
            }
        }
    }
}

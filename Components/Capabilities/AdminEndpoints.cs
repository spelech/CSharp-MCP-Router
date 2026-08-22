using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpRouter.Core.Protocol;
using McpRouter.Core.Routing;
using McpRouter.Infrastructure.Identity;
using McpRouter.Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace McpRouter.Components.Capabilities
{
    /// <summary>
    /// Represents an active Admin SSE client session.
    /// </summary>
    public class AdminSseSession
    {
        public string SessionId { get; }
        public HttpResponse Response { get; }
        public string CallerUsername { get; }
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public AdminSseSession(string sessionId, HttpResponse response, string callerUsername)
        {
            SessionId = sessionId;
            Response = response;
            CallerUsername = callerUsername;
        }

        public async Task WriteMessageAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            await _writeLock.WaitAsync();
            try
            {
                await Response.WriteAsync($"event: message\ndata: {json}\n\n");
                await Response.Body.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }

    /// <summary>
    /// Maps and handles dedicated MCP endpoints for administrative operations (/admin, /admin/sse, /admin/message).
    /// </summary>
    public static class AdminEndpoints
    {
        private static readonly ConcurrentDictionary<string, AdminSseSession> _adminSessions = new();

        public static void RegisterSession(AdminSseSession session)
        {
            _adminSessions[session.SessionId] = session;
        }

        public static void UnregisterSession(string sessionId)
        {
            _adminSessions.TryRemove(sessionId, out _);
        }

        public static AdminSseSession? GetSession(string sessionId)
        {
            _adminSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public static IEndpointRouteBuilder MapAdminMcpEndpoints(this IEndpointRouteBuilder app)
        {
            // 1. /admin (GET/POST/HEAD) guarded by AdminPolicy
            app.MapMethods("/admin", new[] { "GET", "POST", "HEAD" }, HandleAdminSse)
               .RequireAuthorization("AdminPolicy");

            // 2. /admin/sse (GET/POST/HEAD) guarded by AdminPolicy
            app.MapMethods("/admin/sse", new[] { "GET", "POST", "HEAD" }, HandleAdminSse)
               .RequireAuthorization("AdminPolicy");

            // 3. /admin/message (POST) guarded by AdminPolicy
            app.MapPost("/admin/message", HandleAdminMessage)
               .RequireAuthorization("AdminPolicy");

            return app;
        }

        private static async Task HandleAdminSse(
            HttpContext httpContext,
            [FromServices] AdminMcpServer adminMcpServer,
            [FromServices] CompositeIdentityProvider identityProvider,
            ILogger<Program> logger)
        {
            httpContext.Response.Headers.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            if (httpContext.Request.Method == "HEAD")
            {
                return;
            }

            var identity = await identityProvider.ResolveIdentityAsync(httpContext);
            var callerUsername = identity?.Username ?? httpContext.User?.Identity?.Name ?? "admin";

            string requestBody = string.Empty;
            string method = string.Empty;
            JsonElement? id = null;

            if (httpContext.Request.Method == "POST")
            {
                try
                {
                    httpContext.Request.EnableBuffering();
                    using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(requestBody))
                    {
                        using var doc = JsonDocument.Parse(requestBody);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("method", out var methodProp))
                        {
                            method = methodProp.GetString() ?? string.Empty;
                        }
                        if (root.TryGetProperty("id", out var idProp))
                        {
                            id = idProp.Clone();
                        }
                        logger.LogDebug("[JSON-RPC Admin Client -> Gateway] Method: {Method}", method?.Replace(Environment.NewLine, "")?.Replace("\n", "")?.Replace("\r", ""));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse POST body for /admin endpoint");
                }
            }

            bool isSseAccept = httpContext.Request.Headers.Accept.ToString().Contains("text/event-stream");
            bool isInitializeOrDiscover = method == "initialize" || method == "server/discover";

            // Direct JSON response for non-SSE POST requests
            if (httpContext.Request.Method == "POST" && !isSseAccept && !string.IsNullOrEmpty(method) && !isInitializeOrDiscover)
            {
                try
                {
                    var jsonRpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody)
                        ?? new JsonRpcRequest
                        {
                            Method = method,
                            Id = id != null ? (id.Value.ValueKind == JsonValueKind.Number ? (object)id.Value.GetInt64() : id.Value.GetString()) : null
                        };

                    var result = await adminMcpServer.ProcessRequestAsync(jsonRpcReq, callerUsername);
                    httpContext.Response.Headers.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(result);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing direct JSON-RPC admin request: {Method}", method);
                    httpContext.Response.Headers.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        jsonrpc = "2.0",
                        id = id != null ? (object)id : null,
                        error = new { code = -32603, message = "An unexpected error occurred." }
                    });
                    return;
                }
            }

            var sessionId = Guid.NewGuid().ToString("N");
            logger.LogInformation("New Admin SSE connection ({Method}). SessionId: {SessionId}, User: {User}",
                httpContext.Request.Method, sessionId, callerUsername?.Replace(Environment.NewLine, "")?.Replace("\n", "")?.Replace("\r", ""));

            var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
            if (string.IsNullOrEmpty(scheme)) scheme = httpContext.Request.Scheme;
            var host = httpContext.Request.Host.Value;
            var absoluteUrl = $"{scheme}://{host}/admin/message?sessionId={sessionId}";

            await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
            await httpContext.Response.Body.FlushAsync();

            var sseSession = new AdminSseSession(sessionId, httpContext.Response, callerUsername ?? "anonymous");
            RegisterSession(sseSession);

            if (httpContext.Request.Method == "POST" && isInitializeOrDiscover)
            {
                try
                {
                    var jsonRpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody)
                        ?? new JsonRpcRequest
                        {
                            Method = method,
                            Id = id != null ? (id.Value.ValueKind == JsonValueKind.Number ? (object)id.Value.GetInt64() : id.Value.GetString()) : null
                        };

                    var rpcResponse = await adminMcpServer.ProcessRequestAsync(jsonRpcReq, callerUsername);
                    await sseSession.WriteMessageAsync(rpcResponse);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process initial message in POST /admin SessionId: {SessionId}", sessionId);
                }
            }

            try
            {
                while (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(15000, httpContext.RequestAborted);
                    await httpContext.Response.WriteAsync(":ping\n\n");
                    await httpContext.Response.Body.FlushAsync();
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Admin SSE connection closed for SessionId: {SessionId}", sessionId);
            }
            finally
            {
                UnregisterSession(sessionId);
            }
        }

        private static async Task<IResult> HandleAdminMessage(
            HttpContext httpContext,
            [FromQuery] string sessionId,
            [FromServices] AdminMcpServer adminMcpServer,
            [FromServices] CompositeIdentityProvider identityProvider,
            ILogger<Program> logger)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Results.BadRequest(new { error = "Missing required 'sessionId' query parameter." });
            }

            var session = GetSession(sessionId);
            if (session == null)
            {
                for (int i = 0; i < 20; i++)
                {
                    await Task.Delay(50);
                    session = GetSession(sessionId);
                    if (session != null) break;
                }
            }

            if (session == null)
            {
                return Results.NotFound(new { error = "Session not found." });
            }

            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return Results.BadRequest(new { error = "Request body cannot be empty." });
            }


            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("method", out var methodProp))
                {
                    return Results.BadRequest(new { error = "Invalid JSON-RPC: missing 'method' property." });
                }

                var method = methodProp.GetString() ?? string.Empty;
            logger.LogDebug("[JSON-RPC Admin Client -> Gateway] Method: {Method}", method?.Replace(Environment.NewLine, "")?.Replace("\n", "")?.Replace("\r", ""));
                var id = root.TryGetProperty("id", out var idProp) ? idProp.Clone() : (JsonElement?)null;
                var identity = await identityProvider.ResolveIdentityAsync(httpContext);
                var callerUsername = identity?.Username ?? session.CallerUsername ?? "admin";

                var jsonRpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(body);
                if (jsonRpcReq == null)
                {
                    jsonRpcReq = new JsonRpcRequest
                    {
                        Method = method,
                        Id = id != null ? (id.Value.ValueKind == JsonValueKind.Number ? (object)id.Value.GetInt64() : id.Value.GetString()) : null,
                        Params = root.TryGetProperty("params", out var p) ? p.Clone() : null
                    };
                }

                var response = await adminMcpServer.ProcessRequestAsync(jsonRpcReq, callerUsername);
                await session.WriteMessageAsync(response);

                return Results.Accepted();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing admin message for sessionId {SessionId}", sessionId);
                return Results.Problem("An unexpected error occurred.");
            }
        }
    }
}

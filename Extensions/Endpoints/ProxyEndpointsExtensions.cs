using McpRouter.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Services;
using Dapper;
using System.Reflection;
using System.Linq;

namespace McpRouter.Extensions
{
    public static class ProxyEndpointsExtensions
    {
        private static readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";

        public static void MapProxyEndpoints(this WebApplication app)
        {
// ----------------------------------------------------
            // MCP CLIENT SSE HANDLER
            // ----------------------------------------------------
            app.MapMethods("/sse", new[] { "GET", "POST", "HEAD" }, async (HttpContext httpContext, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";
            
                if (httpContext.Request.Method == "HEAD")
                {
                    return;
                }
            
                // Read body if POST
                string requestBody = string.Empty;
                string method = string.Empty;
                JsonElement? id = null;
                if (httpContext.Request.Method == "POST")
                {
                    try
                    {
                        httpContext.Request.EnableBuffering();
                        using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                            httpContext.Request.Body.Position = 0;
                        }
            
                        if (!string.IsNullOrEmpty(requestBody))
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
                            logger.LogDebug("[JSON-RPC Client -> Gateway] {Payload}", McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(requestBody));
                        }
                    }
                    catch (UnauthorizedAccessException exAuth)
                    {
                        logger.LogWarning(exAuth, "Unauthorized access during stateless request handling");
                        httpContext.Response.StatusCode = 403;
                        httpContext.Response.Headers.ContentType = "application/json";
                        await httpContext.Response.WriteAsJsonAsync(new {
                            jsonrpc = "2.0",
                            error = new { code = -32001, message = exAuth.Message }
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse POST /sse body");
                    }
                }

                // Determine if this is a subsequent request for an existing stateless/global session
                bool isSubsequentRequest = httpContext.Request.Method == "POST" && 
                                           method != "initialize" && 
                                           method != "server/discover";

                if (isSubsequentRequest)
                {
                    var globalSessionId = "global-stateless-session";
                    var activeSession = sessionManager.GetSession(globalSessionId);
                    if (activeSession == null)
                    {
                        logger.LogWarning("Global session not found for stateless request: {Method}", method);
                        httpContext.Response.StatusCode = 404;
                        await httpContext.Response.WriteAsJsonAsync(new { error = "Session not found." });
                        return;
                    }

                    sessionManager.IncrementTotalRequests();
                    logger.LogInformation("Routing stateless POST /sse request method {Method} to global session", method);
                    try
                    {
                        if (string.IsNullOrEmpty(method))
                        {
                            if (id != null)
                            {
                                var idStr = id.Value.GetString() ?? id.Value.GetRawText();
                                if (activeSession.TryHandleClientResponse(idStr, requestBody))
                                {
                                    httpContext.Response.StatusCode = 202;
                                    return;
                                }
                            }
                            httpContext.Response.StatusCode = 400;
                            return;
                        }

                        if (method == "notifications/cancelled")
                        {
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("requestId", out var reqIdProp))
                            {
                                var reqId = reqIdProp.GetString() ?? reqIdProp.GetRawText();
                                activeSession.CancelRequest(reqId);
                            }
                            httpContext.Response.StatusCode = 202;
                            return;
                        }

                        if (method == "tools/list")
                        {
                            var tools = await activeSession.ListToolsAsync(requestBody, httpContext);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new { tools }
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else if (method == "tools/call")
                        {
                            var dbFactory = httpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("name", out var nameProp))
                            {
                                var toolName = nameProp.GetString() ?? string.Empty;
                                var res = await activeSession.CallToolAsync(toolName, requestBody, dbFactory, httpContext);
                                var response = new
                                {
                                    jsonrpc = "2.0",
                                    id = id != null ? (object)id : null,
                                    result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                                };
                                httpContext.Response.Headers.ContentType = "application/json";
                                await httpContext.Response.WriteAsJsonAsync(response);
                                return;
                            }
                            httpContext.Response.StatusCode = 400;
                            return;
                        }
                        else if (method == "resources/list")
                        {
                            var resources = await activeSession.ListResourcesAsync(requestBody, httpContext);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new { resources }
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else if (method == "resources/templates/list")
                        {
                            var templates = await activeSession.ListResourceTemplatesAsync(requestBody);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new { templates }
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else if (method == "resources/read")
                        {
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("uri", out var uriProp))
                            {
                                var uri = uriProp.GetString() ?? string.Empty;
                                var res = await activeSession.ReadResourceAsync(uri, requestBody, httpContext);
                                var response = new
                                {
                                    jsonrpc = "2.0",
                                    id = id != null ? (object)id : null,
                                    result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                                };
                                httpContext.Response.Headers.ContentType = "application/json";
                                await httpContext.Response.WriteAsJsonAsync(response);
                                return;
                            }
                            httpContext.Response.StatusCode = 400;
                            return;
                        }
                        else if (method == "prompts/list")
                        {
                            var prompts = await activeSession.ListPromptsAsync(requestBody, httpContext);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new { prompts }
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else if (method == "prompts/get")
                        {
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("name", out var nameProp))
                            {
                                var name = nameProp.GetString() ?? string.Empty;
                                var res = await activeSession.GetPromptAsync(name, requestBody, httpContext);
                                var response = new
                                {
                                    jsonrpc = "2.0",
                                    id = id != null ? (object)id : null,
                                    result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                                };
                                httpContext.Response.Headers.ContentType = "application/json";
                                await httpContext.Response.WriteAsJsonAsync(response);
                                return;
                            }
                            httpContext.Response.StatusCode = 400;
                            return;
                        }
                        else if (method == "completion/complete")
                        {
                            var res = await activeSession.CompleteAsync(requestBody);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = res
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else if (method == "roots/list")
                        {
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new {
                                    roots = new[] {
                                        new {
                                            uri = "file:///containers",
                                            name = "Docker Containers Workspace"
                                        }
                                    }
                                }
                            };
                            httpContext.Response.Headers.ContentType = "application/json";
                            await httpContext.Response.WriteAsJsonAsync(response);
                            return;
                        }
                        else
                        {
                            await activeSession.BroadcastNotificationAsync(method, requestBody);
                            httpContext.Response.StatusCode = 202;
                            return;
                        }
                    }
                    catch (UnauthorizedAccessException exAuth)
                    {
                        logger.LogWarning(exAuth, "Unauthorized access during stateless request handling");
                        httpContext.Response.StatusCode = 403;
                        httpContext.Response.Headers.ContentType = "application/json";
                        await httpContext.Response.WriteAsJsonAsync(new {
                            jsonrpc = "2.0",
                            error = new { code = -32001, message = exAuth.Message }
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error routing stateless message");
                        httpContext.Response.StatusCode = 500;
                        return;
                    }
                }

                // Otherwise, this is a new session establishment request (GET /sse or POST with initialize/discover)
                var sessionId = (httpContext.Request.Method == "POST") ? "global-stateless-session" : Guid.NewGuid().ToString("N");
                logger.LogInformation("New client SSE connection ({Method}). SessionId: {SessionId}", httpContext.Request.Method, sessionId);
            
                // Write SSE endpoint event
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme)) scheme = httpContext.Request.Scheme;
                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();
            
                bool metaMode = httpContext.Request.Query["meta"] != "false";
                
                // Retrieve or create session
                ClientSession session;
                var existingSession = sessionManager.GetSession(sessionId);
                if (existingSession != null)
                {
                    session = existingSession;
                }
                else
                {
                    session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response, targetServerId: null, metaMode);
                }
            
                if (httpContext.Request.Method == "POST")
                {
                    if (method == "initialize")
                    {
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                protocolVersion = "2024-11-05",
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new { name = "McpRouterGateway", version = AppVersion }
                            }
                        };
                        var json = JsonSerializer.Serialize(response);
                        await httpContext.Response.WriteAsync($"event: message\ndata: {json}\n\n");
                        await httpContext.Response.Body.FlushAsync();
                        session.StartInitialization(requestBody);
                    }
                    else if (method == "server/discover")
                    {
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                supportedVersions = new[] { "2026-07-28" },
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new { name = "McpRouterGateway", version = AppVersion }
                            }
                        };
                        var json = JsonSerializer.Serialize(response);
                        await httpContext.Response.WriteAsync($"event: message\ndata: {json}\n\n");
                        await httpContext.Response.Body.FlushAsync();
                        session.StartInitialization(requestBody);
                    }
                }
            
                // Keep connection alive
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
                    logger.LogInformation("Client SSE connection closed for SessionId: {SessionId}", sessionId);
                }
                finally
                {
                    if (sessionId != "global-stateless-session")
                    {
                        sessionManager.CloseSession(sessionId);
                    }
                }
            }).RequireAuthorization();
            
            // Minimal API route for handling GET (SSE initialization) and POST (JSON-RPC requests)
            app.MapMethods("/{targetServerId:regex(^[a-zA-Z0-9_-]+$)}", new[] { "GET", "POST", "HEAD" }, async (HttpContext httpContext, [FromServices] SessionManager sessionManager, ILogger<Program> logger, string targetServerId) =>
            {
                // RBAC Check for targetServerId
                var compositeProvider = httpContext.RequestServices.GetRequiredService<McpRouter.Core.Identity.CompositeIdentityProvider>();
                var identity = await compositeProvider.ResolveIdentityAsync(httpContext);

                if (!McpRouter.Core.Security.SecurityValidationHelper.IsAdmin(identity, httpContext.RequestServices.GetService<IConfiguration>()))
                {
                    var dbFactory = httpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                    using var conn = dbFactory.CreateConnection();
                    var targetServerKey = $"server:{targetServerId}";

                    if (dbFactory.ProviderName == "sqlite")
                    {
                        const string countSql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId = @TargetId;";
                        int policyCount = await conn.ExecuteScalarAsync<int>(countSql, new { TargetId = targetServerKey });
                        if (policyCount == 0)
                        {
                            httpContext.Response.StatusCode = 403;
                            var audit = httpContext.RequestServices.GetService<McpRouter.Core.Logging.IAuditLogger>();
                            if (audit != null)
                            {
                                await audit.LogInvocationAsync(
                                    Guid.NewGuid().ToString("N"),
                                    identity.Username,
                                    identity.Sid ?? "",
                                    targetServerId,
                                    "server/connect",
                                    httpContext.Request.Method,
                                    0,
                                    403,
                                    errorMessage: "Access denied"
                                );
                            }
                            await httpContext.Response.WriteAsJsonAsync(new { error = $"Access denied to target server: {targetServerId}" });
                            return;
                        }

                        const string denySql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId = @TargetId AND RequiredGroup IN @GroupNames AND IsAllowed = 0;";
                        int denyCount = await conn.ExecuteScalarAsync<int>(denySql, new { TargetId = targetServerKey, GroupNames = identity.GroupNames });

                        const string allowSql = "SELECT COUNT(*) FROM AccessPolicies WHERE TargetId = @TargetId AND RequiredGroup IN @GroupNames AND IsAllowed = 1;";
                        int allowCount = await conn.ExecuteScalarAsync<int>(allowSql, new { TargetId = targetServerKey, GroupNames = identity.GroupNames });

                        if (denyCount > 0 || allowCount == 0)
                        {
                            httpContext.Response.StatusCode = 403;
                            var audit = httpContext.RequestServices.GetService<McpRouter.Core.Logging.IAuditLogger>();
                            if (audit != null)
                            {
                                await audit.LogInvocationAsync(
                                    Guid.NewGuid().ToString("N"),
                                    identity.Username,
                                    identity.Sid ?? "",
                                    targetServerId,
                                    "server/connect",
                                    httpContext.Request.Method,
                                    0,
                                    403,
                                    errorMessage: "Access denied"
                                );
                            }
                            await httpContext.Response.WriteAsJsonAsync(new { error = $"Access denied to target server: {targetServerId}" });
                            return;
                        }
                    }
                    else
                    {
                        var groupNamesCsv = string.Join(",", identity.GroupNames);
                        var parameters = new {
                            GroupNames = groupNamesCsv,
                            ItemName = targetServerId,
                            RequestMethod = "GET"
                        };
                        int isAllowed = await conn.ExecuteScalarAsync<int>(
                            "sp_EvaluateUserAccess",
                            parameters,
                            commandType: System.Data.CommandType.StoredProcedure
                        );
                        if (isAllowed == 0)
                        {
                            httpContext.Response.StatusCode = 403;
                            var audit = httpContext.RequestServices.GetService<McpRouter.Core.Logging.IAuditLogger>();
                            if (audit != null)
                            {
                                await audit.LogInvocationAsync(
                                    Guid.NewGuid().ToString("N"),
                                    identity.Username,
                                    identity.Sid ?? "",
                                    targetServerId,
                                    "server/connect",
                                    httpContext.Request.Method,
                                    0,
                                    403,
                                    errorMessage: "Access denied"
                                );
                            }
                            await httpContext.Response.WriteAsJsonAsync(new { error = $"Access denied to target server: {targetServerId}" });
                            return;
                        }
                    }
                }

                var isSse = httpContext.Request.Headers.Accept.ToString().Contains("text/event-stream");
                var isPost = HttpMethods.IsPost(httpContext.Request.Method);
                bool metaMode = httpContext.Request.Query["meta"] == "true";
            
                // Session id is an opaque server-issued capability, NEVER the caller's token.
                // Clients receive it in the SSE `endpoint` URL and echo it back on /mcp/message.
                string sessionId = Guid.NewGuid().ToString("N");
            
                // Read body if POST
                string requestBody = string.Empty;
                string method = string.Empty;
                JsonElement? id = null;


            
                if (httpContext.Request.Method == "POST")
                {
                    try
                    {
                        httpContext.Request.EnableBuffering();
                        using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                            httpContext.Request.Body.Position = 0;
                        }
            
                        if (!string.IsNullOrEmpty(requestBody))
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
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse POST message body for /mcp");
                    }
                }
            

            
                // Otherwise, establish a new SSE stream (for GET requests, or POST with method "initialize")
                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";
            
                logger.LogInformation("New client /mcp SSE connection ({Method}). SessionId: {SessionId}", httpContext.Request.Method, sessionId);
            
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme)) scheme = httpContext.Request.Scheme;
                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/mcp/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();
            
                var session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response, targetServerId, metaMode);
            
                if (httpContext.Request.Method == "POST" && (method == "initialize" || method == "server/discover"))
                {
                    try
                    {
                        logger.LogDebug("Processing initial JSON-RPC message in POST /mcp body: {Body}", McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(requestBody));
                        var serverName = "McpRouterGateway";
                        if (!string.IsNullOrWhiteSpace(targetServerId))
                        {
                            var dbFactory = httpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                            using var dbConn = dbFactory.CreateConnection();
                            var rawServers = await dbConn.QueryAsync<McpServer>("SELECT * FROM Servers");
                            var allServers = rawServers.ToList();
                            var targetServer = allServers.FirstOrDefault(s => s.Id == targetServerId);
                            if (targetServer != null)
                            {
                                serverName = targetServer.DisplayName;
                            }
                            else if (allServers.Any(s => s.Categories != null && s.Categories.Contains(targetServerId)))
                            {
                                // Fallback to Category name
                                serverName = char.ToUpper(targetServerId[0]) + targetServerId.Substring(1) + " Services";
                            }
                        }
                        var response = method == "server/discover" ? (object)new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                supportedVersions = new[] { "2026-07-28" },
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new { name = serverName, version = AppVersion }
                            }
                        } : new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                protocolVersion = "2024-11-05",
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new { name = serverName, version = AppVersion }
                            }
                        };
                        await session.WriteMessageAsync(response);
                        session.StartInitialization(requestBody);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to initialize POST message body for /mcp SessionId: {SessionId}", sessionId);
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
                    logger.LogInformation("/mcp connection closed for SessionId: {SessionId}", sessionId);
                }
                finally
                {
                    sessionManager.CloseSession(sessionId);
                }
            }).RequireAuthorization();
            
            // ----------------------------------------------------
            // MCP CLIENT MESSAGE ROUTER
            // ----------------------------------------------------
            var handleMessage = async (HttpContext httpContext, string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                var session = sessionManager.GetSession(sessionId);
                if (session == null)
                {
                    // Retry for up to 1 second to handle the asynchronous initialization race
                    for (int i = 0; i < 20; i++)
                    {
                        await Task.Delay(50);
                        session = sessionManager.GetSession(sessionId);
                        if (session != null) break;
                    }
                }
                if (session == null)
                {
                    return Results.NotFound(new { error = "Session not found." });
                }
                sessionManager.IncrementTotalRequests();
            
                using var reader = new StreamReader(httpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                
                logger.LogDebug("[JSON-RPC Client -> Gateway] {Payload}", McpRouter.Core.Logging.PiiSanitizer.SanitizePayload(body));
            
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    
                    if (!root.TryGetProperty("method", out var methodProp))
                    {
                        if (root.TryGetProperty("id", out var idPropClient))
                        {
                            var idStr = idPropClient.GetString() ?? idPropClient.GetRawText();
                            if (session.TryHandleClientResponse(idStr, body))
                            {
                                return Results.Accepted();
                            }
                        }
                        return Results.BadRequest(new { error = "Invalid JSON-RPC: missing method" });
                    }
            
                    var method = methodProp.GetString() ?? string.Empty;
                    var id = root.TryGetProperty("id", out var idProp) ? idProp.Clone() : (JsonElement?)null;
            
                    if (method == "initialize")
                    {
                        // Respond directly matching the standard server information
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                protocolVersion = "2024-11-05",
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new
                                {
                                    name = "McpRouterGateway",
                                    version = AppVersion
                                }
                            }
                        };
                        
                        // Write SSE event to client stream
                        await session.WriteMessageAsync(response);
                        
                        session.StartInitialization(body);
                        
                        return Results.Accepted();
                    }
                    else if (method == "server/discover")
                    {
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                supportedVersions = new[] { "2026-07-28" },
                                capabilities = new
                                {
                                    tools = new { listChanged = true },
                                    prompts = new { listChanged = true },
                                    resources = new { subscribe = false, listChanged = true }
                                },
                                serverInfo = new
                                {
                                    name = "McpRouterGateway",
                                    version = AppVersion
                                }
                            }
                        };
                        
                        await session.WriteMessageAsync(response);
                        session.StartInitialization(body);
                        return Results.Accepted();
                    }
                    else if (method == "tools/list")
                    {
                        var tools = await session.ListToolsAsync(body, httpContext);
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new { tools }
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "tools/call")
                    {
                        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("name", out var nameProp))
                        {
                            var toolName = nameProp.GetString() ?? string.Empty;
                            var dbFactory = httpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                            var res = await session.CallToolAsync(toolName, body, dbFactory, httpContext);
                            
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                            };
                            await session.WriteMessageAsync(response);
                            return Results.Accepted();
                        }
                        return Results.BadRequest(new { error = "Invalid tools/call: missing name parameter" });
                    }
                    else if (method == "resources/list")
                    {
                        var resources = await session.ListResourcesAsync(body, httpContext);
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new { resources }
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "resources/read")
                    {
                        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("uri", out var uriProp))
                        {
                            var uri = uriProp.GetString() ?? string.Empty;
                            var res = await session.ReadResourceAsync(uri, body, httpContext);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                            };
                            await session.WriteMessageAsync(response);
                            return Results.Accepted();
                        }
                        return Results.BadRequest(new { error = "Invalid resources/read: missing uri parameter" });
                    }
                    else if (method == "resources/templates/list")
                    {
                        var templates = await session.ListResourceTemplatesAsync(body);
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new { templates }
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "completion/complete")
                    {
                        var res = await session.CompleteAsync(body);
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = res
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "roots/list")
                    {
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new {
                                roots = new[] {
                                    new {
                                        uri = "file:///containers",
                                        name = "Docker Containers Workspace"
                                    }
                                }
                            }
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "prompts/list")
                    {
                        var prompts = await session.ListPromptsAsync(body, httpContext);
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new { prompts }
                        };
                        await session.WriteMessageAsync(response);
                        return Results.Accepted();
                    }
                    else if (method == "prompts/get")
                    {
                        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("name", out var nameProp))
                        {
                            var name = nameProp.GetString() ?? string.Empty;
                            var res = await session.GetPromptAsync(name, body, httpContext);
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = res is JsonElement je && je.TryGetProperty("result", out var r) ? (object)r : res
                            };
                            await session.WriteMessageAsync(response);
                            return Results.Accepted();
                        }
                        return Results.BadRequest(new { error = "Invalid prompts/get: missing name parameter" });
                    }
                    else if (method == "notifications/cancelled")
                    {
                        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("requestId", out var reqIdProp))
                        {
                            var reqId = reqIdProp.GetString() ?? reqIdProp.GetRawText();
                            session.CancelRequest(reqId);
                        }
                        await session.BroadcastNotificationAsync(method, body);
                        return Results.Accepted();
                    }
                    else if (method.StartsWith("notifications/"))
                    {
                        await session.BroadcastNotificationAsync(method, body);
                        return Results.Accepted();
                    }
                    else
                    {
                        // Forward other JSON-RPC requests (like resources/list, prompts/list) directly to all backends, returning combined or first valid
                        // In a router, we route based on the request method
                        logger.LogWarning("Method {Method} not explicitly handled by Router; forwarding to active backends", method);
                        var results = await session.BroadcastRequestAsync(body);
                        if (results.Count > 0 && id != null)
                        {
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = (object)id,
                                result = results.First().Value
                            };
                            await session.WriteMessageAsync(response);
                        }
                        return Results.Accepted();
                    }
                }
                catch (UnauthorizedAccessException exAuth)
                {
                    logger.LogWarning(exAuth, "Unauthorized access during client message routing");
                    httpContext.Response.StatusCode = 403;
                    return Results.Json(new {
                        jsonrpc = "2.0",
                        error = new { code = -32001, message = exAuth.Message }
                    }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error routing client message.");
                    return Results.Problem(ex.Message);
                }
            };
            
            app.MapPost("/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) => 
                await handleMessage(httpContext, sessionId, sessionManager, logger)).RequireAuthorization();
            
            app.MapPost("/mcp/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) => 
                await handleMessage(httpContext, sessionId, sessionManager, logger)).RequireAuthorization();
            
            
        }
    }
}
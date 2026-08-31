using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace ModelContextGateway.Components.Capabilities
{
    public static class ProxyEndpoints
    {
        private static readonly string AppVersion = GatewayMetadata.Version;

        public static IEndpointRouteBuilder MapProxyEndpoints(this IEndpointRouteBuilder app)
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
                            var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(root);
                            McpLogLevelHelper.CurrentPerRequestLogLevel.Value = logLevel;
                            httpContext.Items["PerRequestLogLevel"] = logLevel;
                            if (root.TryGetProperty("method", out var methodProp))
                            {
                                method = methodProp.GetString() ?? string.Empty;
                            }
                            if (root.TryGetProperty("id", out var idProp))
                            {
                                id = idProp.Clone();
                            }
                            logger.LogDebug("[JSON-RPC Client -> Gateway] {Payload}", PiiSanitizer.SanitizePayload(requestBody));
                        }
                    }
                    catch (UnauthorizedAccessException exAuth)
                    {
                        logger.LogWarning(exAuth, "Unauthorized access during stateless request handling");
                        httpContext.Response.StatusCode = 403;
                        httpContext.Response.Headers.ContentType = "application/json";
                        await httpContext.Response.WriteAsJsonAsync(new
                        {
                            jsonrpc = "2.0",
                            error = new { code = McpErrorCodes.ConnectionClosed, message = exAuth.Message }
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
                                var idStr = id.Value.ValueKind == JsonValueKind.String ? id.Value.GetString() : id.Value.GetRawText();
                                if (idStr != null && activeSession.TryHandleClientResponse(idStr, requestBody))
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
                                var reqId = reqIdProp.ValueKind == JsonValueKind.String ? reqIdProp.GetString() : reqIdProp.GetRawText();
                                if (!string.IsNullOrEmpty(reqId))
                                {
                                    activeSession.CancelRequest(reqId, httpContext.TraceIdentifier);
                                }
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
                            var templates = await activeSession.ListResourceTemplatesAsync(requestBody, httpContext);
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
                            var res = await activeSession.CompleteAsync(requestBody, httpContext);
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
                            logger.LogWarning("[Deprecated Spec MCP 2026-07-28] Method 'roots/list' is deprecated and scheduled for removal in future specification versions.");
                            var response = new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                result = new
                                {
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
                        await httpContext.Response.WriteAsJsonAsync(new
                        {
                            jsonrpc = "2.0",
                            error = new { code = McpErrorCodes.ConnectionClosed, message = exAuth.Message }
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
                logger.LogWarning("[Deprecated Spec MCP 2026-07-28] HTTP+SSE transport (/sse) is reclassified as Deprecated; recommend migration to Streamable HTTP.");
                logger.LogInformation("New client SSE connection ({Method}). SessionId: {SessionId}", httpContext.Request.Method, sessionId);

                // Write SSE endpoint event
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme))
                {
                    scheme = httpContext.Request.Scheme;
                }

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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
                                },
                                serverInfo = new { name = "ModelContextGateway", version = AppVersion }
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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
                                },
                                serverInfo = new { name = "ModelContextGateway", version = AppVersion }
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
                // Target Routing for router-admin and admin virtual server
                if (string.Equals(targetServerId, "router-admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(targetServerId, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleTargetAdminAsync(httpContext, targetServerId);
                    return;
                }
                if (string.Equals(targetServerId, "consent", StringComparison.OrdinalIgnoreCase))
                {
                    httpContext.Response.ContentType = "text/html";
                    var indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
                    if (File.Exists(indexPath))
                    {
                        await httpContext.Response.SendFileAsync(indexPath);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 404;
                    }
                    return;
                }
                // First check AppKey authorization if authenticated via AppKey
                if (httpContext.Items.TryGetValue("AppKeyUsed", out var appKeyUsedObj) == true && appKeyUsedObj is bool appKeyUsed && appKeyUsed)
                {
                    if (httpContext.Items.TryGetValue("AppKeyScopes", out var scopesObj) == true && scopesObj is string scopesJson)
                    {
                        bool scopeAllowed = false;
                        try
                        {
                            var scopes = JsonSerializer.Deserialize<List<string>>(scopesJson);
                            if (scopes != null)
                            {
                                var dbFactory = httpContext.RequestServices.GetService<IDbConnectionFactory>();
                                List<string>? serverCategories = null;
                                if (dbFactory != null)
                                {
                                    try
                                    {
                                        using var dbConn = dbFactory.CreateConnection();
                                        var rawCat = await dbConn.ExecuteScalarAsync<string>("SELECT Categories FROM Servers WHERE Id = @Id", new { Id = targetServerId });
                                        if (!string.IsNullOrEmpty(rawCat))
                                        {
                                            try { serverCategories = JsonSerializer.Deserialize<List<string>>(rawCat); }
                                            catch { serverCategories = rawCat.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); }
                                        }
                                    }
                                    catch { }
                                }

                                foreach (var s in scopes)
                                {
                                    var cleanScope = s.Trim().ToLowerInvariant();
                                    if (cleanScope == "all" || cleanScope == "mcp_client" || cleanScope == "*")
                                    {
                                        scopeAllowed = true;
                                        break;
                                    }
                                    if (cleanScope == $"server:{targetServerId}".ToLowerInvariant() || cleanScope == targetServerId.ToLowerInvariant())
                                    {
                                        scopeAllowed = true;
                                        break;
                                    }
                                    if (cleanScope.StartsWith("category:") || cleanScope.StartsWith("group:"))
                                    {
                                        var scopeCategory = cleanScope.StartsWith("category:")
                                            ? cleanScope.Substring("category:".Length).Trim()
                                            : cleanScope.Substring("group:".Length).Trim();

                                        if (!string.IsNullOrEmpty(scopeCategory))
                                        {
                                            if (string.Equals(targetServerId, scopeCategory, StringComparison.OrdinalIgnoreCase))
                                            {
                                                scopeAllowed = true;
                                                break;
                                            }
                                            if (serverCategories != null && serverCategories.Any(c => string.Equals(c, scopeCategory, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                scopeAllowed = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception exScopes)
                        {
                            logger.LogWarning(exScopes, "Failed to parse AppKey scopes JSON: {ScopesJson}", scopesJson);
                        }

                        if (!scopeAllowed)
                        {
                            httpContext.Response.StatusCode = 403;
                            var compositeProvider = httpContext.RequestServices.GetRequiredService<CompositeIdentityProvider>();
                            var identity = await compositeProvider.ResolveIdentityAsync(httpContext);
                            var audit = httpContext.RequestServices.GetService<IAuditLogger>();
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
                else
                {
                    // RBAC Check for targetServerId
                    var compositeProvider = httpContext.RequestServices.GetRequiredService<CompositeIdentityProvider>();
                    var identity = await compositeProvider.ResolveIdentityAsync(httpContext);

                    if (!SecurityValidationHelper.IsAdmin(identity, httpContext.RequestServices.GetService<IConfiguration>()))
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
                                var audit = httpContext.RequestServices.GetService<IAuditLogger>();
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
                                var audit = httpContext.RequestServices.GetService<IAuditLogger>();
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
                            object parameters = dbFactory.ProviderName == "mysql"
                                ? new
                                {
                                    p_GroupNames = groupNamesCsv,
                                    p_ItemName = targetServerId,
                                    p_RequestMethod = "GET"
                                }
                                : new
                                {
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
                                var audit = httpContext.RequestServices.GetService<IAuditLogger>();
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
                }

                var isSse = httpContext.Request.Headers.Accept.ToString().Contains("text/event-stream");
                var isPost = HttpMethods.IsPost(httpContext.Request.Method);
                bool metaMode = httpContext.Request.Query["meta"] == "true";

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
                            var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(root);
                            McpLogLevelHelper.CurrentPerRequestLogLevel.Value = logLevel;
                            httpContext.Items["PerRequestLogLevel"] = logLevel;
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

                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                logger.LogWarning("[Deprecated Spec MCP 2026-07-28] HTTP+SSE transport (/{TargetServerId}) is reclassified as Deprecated; recommend migration to Streamable HTTP.", targetServerId);
                logger.LogInformation("New client /mcp SSE connection ({Method}). SessionId: {SessionId}", httpContext.Request.Method, sessionId);

                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme))
                {
                    scheme = httpContext.Request.Scheme;
                }

                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/mcp/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();

                var session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response, targetServerId, metaMode);

                if (httpContext.Request.Method == "POST" && (method == "initialize" || method == "server/discover"))
                {
                    try
                    {
                        logger.LogDebug("Processing initial JSON-RPC message in POST /mcp body: {Body}", PiiSanitizer.SanitizePayload(requestBody));
                        var serverName = "ModelContextGateway";
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
                            else if (allServers.Any(s => s.Categories != null && s.Categories.Any(c => string.Equals(c, targetServerId, StringComparison.OrdinalIgnoreCase))))
                            {
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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
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
                    for (int i = 0; i < 20; i++)
                    {
                        await Task.Delay(50);
                        session = sessionManager.GetSession(sessionId);
                        if (session != null)
                        {
                            break;
                        }
                    }
                }
                if (session == null)
                {
                    return Results.NotFound(new { error = "Session not found." });
                }
                sessionManager.IncrementTotalRequests();

                using var reader = new StreamReader(httpContext.Request.Body);
                var body = await reader.ReadToEndAsync();

                logger.LogDebug("[JSON-RPC Client -> Gateway] {Payload}", PiiSanitizer.SanitizePayload(body));

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var logLevel = McpLogLevelHelper.ExtractPerRequestLogLevel(root);
                    McpLogLevelHelper.CurrentPerRequestLogLevel.Value = logLevel;
                    httpContext.Items["PerRequestLogLevel"] = logLevel;

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

                    // Protocol Version Header Validation
                    if (httpContext.Request.Headers.TryGetValue("MCP-Protocol-Version", out var protoVersionHeader))
                    {
                        var protoVer = protoVersionHeader.ToString();
                        if (!string.IsNullOrEmpty(protoVer) &&
                            !protoVer.Equals("2026-07-28", StringComparison.OrdinalIgnoreCase) &&
                            !protoVer.Equals("2024-11-05", StringComparison.OrdinalIgnoreCase))
                        {
                            httpContext.Response.StatusCode = 400;
                            return Results.Json(new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                error = new { code = McpErrorCodes.UnsupportedProtocolVersion, message = $"Unsupported protocol version: '{protoVer}'." }
                            }, statusCode: 400);
                        }
                    }

                    // Header Mismatch Validation
                    if (httpContext.Items.TryGetValue("MCP_HEADER_METHOD", out var hMethodObj) && hMethodObj is string hMethod && !string.IsNullOrEmpty(hMethod))
                    {
                        if (!string.Equals(hMethod, method, StringComparison.OrdinalIgnoreCase))
                        {
                            httpContext.Response.StatusCode = 400;
                            return Results.Json(new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                error = new { code = McpErrorCodes.HeaderMismatch, message = $"Header Mcp-Method ('{hMethod}') does not match request body method ('{method}')." }
                            }, statusCode: 400);
                        }
                    }

                    string bodyItemName = string.Empty;
                    if (root.TryGetProperty("params", out var paramsForName))
                    {
                        if (paramsForName.TryGetProperty("name", out var nProp))
                        {
                            bodyItemName = nProp.GetString() ?? string.Empty;
                        }
                        else if (paramsForName.TryGetProperty("uri", out var uProp))
                        {
                            bodyItemName = uProp.GetString() ?? string.Empty;
                        }
                    }

                    if (httpContext.Items.TryGetValue("MCP_HEADER_NAME", out var hNameObj) && hNameObj is string hName && !string.IsNullOrEmpty(hName))
                    {
                        if (!string.IsNullOrEmpty(bodyItemName) && !string.Equals(hName, bodyItemName, StringComparison.OrdinalIgnoreCase))
                        {
                            httpContext.Response.StatusCode = 400;
                            return Results.Json(new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                error = new { code = McpErrorCodes.HeaderMismatch, message = $"Header Mcp-Name ('{hName}') does not match request body item name ('{bodyItemName}')." }
                            }, statusCode: 400);
                        }
                    }

                    if ((method == "initialize" || method == "server/discover") && root.TryGetProperty("params", out var initParamsProp) && initParamsProp.TryGetProperty("protocolVersion", out var pvProp))
                    {
                        var reqPv = pvProp.GetString();
                        if (!string.IsNullOrEmpty(reqPv) &&
                            !reqPv.Equals("2026-07-28", StringComparison.OrdinalIgnoreCase) &&
                            !reqPv.Equals("2024-11-05", StringComparison.OrdinalIgnoreCase))
                        {
                            httpContext.Response.StatusCode = 400;
                            return Results.Json(new
                            {
                                jsonrpc = "2.0",
                                id = id != null ? (object)id : null,
                                error = new { code = McpErrorCodes.UnsupportedProtocolVersion, message = $"Unsupported protocol version: '{reqPv}'." }
                            }, statusCode: 400);
                        }
                    }

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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
                                },
                                serverInfo = new
                                {
                                    name = "ModelContextGateway",
                                    version = AppVersion
                                }
                            }
                        };

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
                                    resources = new { subscribe = false, listChanged = true },
                                    extensions = new { }
                                },
                                serverInfo = new
                                {
                                    name = "ModelContextGateway",
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
                        var templates = await session.ListResourceTemplatesAsync(body, httpContext);
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
                        var res = await session.CompleteAsync(body, httpContext);
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
                        logger.LogWarning("[Deprecated Spec MCP 2026-07-28] Method 'roots/list' is deprecated and scheduled for removal in future specification versions.");
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
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
                            var reqId = reqIdProp.ValueKind == JsonValueKind.String ? reqIdProp.GetString() : reqIdProp.GetRawText();
                            if (!string.IsNullOrEmpty(reqId))
                            {
                                session.CancelRequest(reqId, httpContext?.TraceIdentifier);
                            }
                        }
                        await session.BroadcastNotificationAsync(method, body);
                        return Results.Accepted();
                    }
                    else if (method == "logging/setLevel")
                    {
                        logger.LogWarning("[Deprecated Spec MCP 2026-07-28] Method 'logging/setLevel' (Logging) is deprecated and scheduled for removal in future specification versions.");
                        await session.BroadcastNotificationAsync(method, body);
                        return Results.Accepted();
                    }
                    else if (method.StartsWith("notifications/"))
                    {
                        if (method == "notifications/message" || method.StartsWith("notifications/message/"))
                        {
                            logger.LogWarning("[Deprecated Spec MCP 2026-07-28] Notification method '{Method}' (Logging) is deprecated and scheduled for removal in future specification versions.", method);
                        }
                        await session.BroadcastNotificationAsync(method, body);
                        return Results.Accepted();
                    }
                    else
                    {
                        logger.LogWarning("Method {Method} not explicitly handled by Router; forwarding to active backends", method);
                        if (id == null)
                        {
                            await session.BroadcastNotificationAsync(method, body);
                        }
                        else
                        {
                            var results = await session.BroadcastRequestAsync(body);
                            if (results.Count > 0)
                            {
                                var response = new
                                {
                                    jsonrpc = "2.0",
                                    id = (object)id,
                                    result = results.First().Value
                                };
                                await session.WriteMessageAsync(response);
                            }
                        }
                        return Results.Accepted();
                    }
                }
                catch (UnauthorizedAccessException exAuth)
                {
                    logger.LogWarning(exAuth, "Unauthorized access during client message routing");
                    httpContext.Response.StatusCode = 403;
                    return Results.Json(new
                    {
                        jsonrpc = "2.0",
                        error = new { code = McpErrorCodes.ConnectionClosed, message = exAuth.Message }
                    }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error routing client message.");
                    return Results.Problem("An unexpected error occurred.");
                }
            };

            app.MapPost("/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
                await handleMessage(httpContext, sessionId, sessionManager, logger)).RequireAuthorization();

            app.MapPost("/mcp/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
                await handleMessage(httpContext, sessionId, sessionManager, logger)).RequireAuthorization();

            return app;
        }

        private static async Task HandleTargetAdminAsync(HttpContext httpContext, string targetServerId)
        {
            var compositeProvider = httpContext.RequestServices.GetRequiredService<CompositeIdentityProvider>();
            var identity = await compositeProvider.ResolveIdentityAsync(httpContext);
            var config = httpContext.RequestServices.GetService<IConfiguration>();

            if (!SecurityValidationHelper.IsAdmin(identity, config, httpContext))
            {
                httpContext.Response.StatusCode = 403;
                var audit = httpContext.RequestServices.GetService<IAuditLogger>();
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
                        errorMessage: "Access denied to admin server"
                    );
                }
                await httpContext.Response.WriteAsJsonAsync(new { error = $"Access denied to admin server: {targetServerId}" });
                return;
            }

            var adminMcpServer = httpContext.RequestServices.GetRequiredService<AdminMcpServer>();
            var callerUsername = identity.Username ?? "admin";

            if (httpContext.Request.Method == "POST")
            {
                httpContext.Request.EnableBuffering();
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var requestBody = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;

                bool isSseAccept = httpContext.Request.Headers.Accept.ToString().Contains("text/event-stream");
                string method = string.Empty;
                JsonElement? id = null;

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    try
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
                    catch { }
                }

                bool isInitializeOrDiscover = method == "initialize" || method == "server/discover";

                if (!isSseAccept && !string.IsNullOrEmpty(method) && !isInitializeOrDiscover)
                {
                    var jsonRpcReq = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody)
                        ?? new JsonRpcRequest
                        {
                            Method = method,
                            Id = id != null ? (id.Value.ValueKind == JsonValueKind.Number ? (object)id.Value.GetInt64() : id.Value.GetString()) : null
                        };
                    var rpcResponse = await adminMcpServer.ProcessRequestAsync(jsonRpcReq, callerUsername);
                    httpContext.Response.Headers.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(rpcResponse);
                    return;
                }

                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                var sessionId = Guid.NewGuid().ToString("N");
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme))
                {
                    scheme = httpContext.Request.Scheme;
                }

                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/admin/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();

                var sseSession = new AdminSseSession(sessionId, httpContext.Response, callerUsername);
                AdminEndpoints.RegisterSession(sseSession);

                if (isInitializeOrDiscover)
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

                try
                {
                    while (!httpContext.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(15000, httpContext.RequestAborted);
                        await httpContext.Response.WriteAsync(":ping\n\n");
                        await httpContext.Response.Body.FlushAsync();
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    AdminEndpoints.UnregisterSession(sessionId);
                }
            }
            else
            {
                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                if (httpContext.Request.Method == "HEAD")
                {
                    return;
                }

                var sessionId = Guid.NewGuid().ToString("N");
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme))
                {
                    scheme = httpContext.Request.Scheme;
                }

                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/admin/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();

                var sseSession = new AdminSseSession(sessionId, httpContext.Response, callerUsername);
                AdminEndpoints.RegisterSession(sseSession);

                try
                {
                    while (!httpContext.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(15000, httpContext.RequestAborted);
                        await httpContext.Response.WriteAsync(":ping\n\n");
                        await httpContext.Response.Body.FlushAsync();
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    AdminEndpoints.UnregisterSession(sessionId);
                }
            }
        }
    }
}

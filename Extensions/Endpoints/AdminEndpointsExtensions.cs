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
using McpRouter.Models;
using McpRouter.Core.Logging;
using Dapper;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

namespace McpRouter.Extensions
{
    public static class AdminEndpointsExtensions
    {
        private static readonly string AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.6";

        public static void MapAdminEndpoints(this WebApplication app)
        {
// ----------------------------------------------------
            // DCR & OAUTH ENDPOINTS
            // ----------------------------------------------------
            
            var api = app.MapGroup("").RequireAuthorization("AdminPolicy");

            api.MapGet("/api/me", async (HttpContext context, [FromServices] McpRouter.Core.Identity.CompositeIdentityProvider identityProvider) =>
            {
                var identity = await identityProvider.ResolveIdentityAsync(context);
                if (identity == null || identity.Username == "guest" || identity.Username == "anonymous")
                {
                    return Results.Ok(new { authenticated = false });
                }

                return Results.Ok(new
                {
                    authenticated = true,
                    username = identity.Username,
                    name = identity.Username,
                    email = "",
                    groups = identity.GroupNames
                });
            });
            
            
            
            
// --- TEST BENCH & LOGS ENDPOINTS ---

            // 1. Logs API
            api.MapGet("/api/logs", () => Results.Ok(LogBuffer.GetLogs()));
            api.MapDelete("/api/logs", async (HttpContext ctx) => {
                LogBuffer.Clear();
                var audit = ctx.RequestServices.GetService<McpRouter.Core.Logging.IAuditLogger>();
                if (audit != null) await audit.LogAdminActionAsync(ctx.User?.Identity?.Name ?? "unknown", "logs.clear", "InMemoryLogBuffer", "", true);
                return Results.Ok(new { success = true });
            });

            // 1.2 Audit Query API
            api.MapGet("/api/audit", async (HttpContext ctx, [FromServices] IDbConnectionFactory dbf,
                string? user, string? server, DateTime? since, int take = 200, int skip = 0) =>
            {
                take = Math.Clamp(take, 1, 1000);
                using var conn = dbf.CreateConnection();
                string sql;
                if (dbf.ProviderName == "mssql")
                {
                    sql = @"SELECT Timestamp, UserPrincipalName, UserSid, ServerCodeName, ItemName, RequestMethod, StatusCode, ExecutionTimeMs, ErrorMessage
                            FROM AuditLogs
                            WHERE (@user   IS NULL OR UserPrincipalName = @user)
                              AND (@server IS NULL OR ServerCodeName = @server)
                              AND (@since  IS NULL OR Timestamp >= @since)
                            ORDER BY Timestamp DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";
                }
                else
                {
                    sql = @"SELECT Timestamp, UserPrincipalName, UserSid, ServerCodeName, ItemName, RequestMethod, StatusCode, ExecutionTimeMs, ErrorMessage
                            FROM AuditLogs
                            WHERE (@user   IS NULL OR UserPrincipalName = @user)
                              AND (@server IS NULL OR ServerCodeName = @server)
                              AND (@since  IS NULL OR Timestamp >= @since)
                            ORDER BY Timestamp DESC LIMIT @take OFFSET @skip;";
                }
                var rows = await conn.QueryAsync(sql, new { user, server, since, take, skip });
                var audit = ctx.RequestServices.GetService<McpRouter.Core.Logging.IAuditLogger>();
                if (audit != null) await audit.LogAdminActionAsync(ctx.User?.Identity?.Name ?? "unknown", "audit.query", "AuditLogs", "", true);
                return Results.Ok(rows);
            });

            // 1.5. Settings API
            api.MapGet("/api/settings", (DynamicEmbeddingService embeddingService) => 
                Results.Ok(embeddingService.GetSettings()));
                
            api.MapPost("/api/settings", (RouterSettings settings, DynamicEmbeddingService embeddingService, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) => {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                try
                {
                    embeddingService.SaveSettings(settings);
                    _ = auditLogger.LogAdminActionAsync(username, "UpdateSettings", "embedding-settings", System.Text.Json.JsonSerializer.Serialize(settings), true);
                    return Results.Ok(new { success = true, settings = embeddingService.GetSettings() });
                }
                catch (ArgumentException ex)
                {
                    _ = auditLogger.LogAdminActionAsync(username, "UpdateSettings", "embedding-settings", System.Text.Json.JsonSerializer.Serialize(settings), false, ex.Message);
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            api.MapGet("/api/approvals", ([FromServices] SessionManager sessionManager) =>
            {
                var approvals = sessionManager.PendingApprovals.Values.Select(a => new
                {
                    id = a.Id,
                    toolName = a.ToolName,
                    arguments = a.Arguments,
                    sessionId = a.SessionId
                }).ToList();
                return Results.Ok(approvals);
            });

            api.MapGet("/api/diagnostics", ([FromServices] SessionManager sessionManager) => 
            {
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                int fdCount = 0;
                
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    try 
                    {
                        fdCount = System.IO.Directory.GetFiles($"/proc/{proc.Id}/fd").Length;
                    }
                    catch { /* Fallback if restricted */ }
                }
                
                return Results.Ok(new {
                    activeSessions = sessionManager.ActiveSessionsCount,
                    pendingApprovals = sessionManager.PendingApprovals.Count,
                    workingSet64 = proc.WorkingSet64,
                    handleCount = fdCount > 0 ? fdCount : proc.HandleCount
                });
            }).AllowAnonymous();

            api.MapPost("/api/approvals/{id}/action", async ([FromRoute] string id, [FromBody] System.Text.Json.JsonElement body, [FromServices] SessionManager sessionManager, HttpContext httpContext) =>
            {
                if (sessionManager.PendingApprovals.TryRemove(id, out var approval))
                {
                    bool approved = false;
                    if (body.TryGetProperty("approved", out var appProp))
                    {
                        approved = appProp.GetBoolean();
                    }
                    approval.Tcs.SetResult(approved);

                    var audit = httpContext.RequestServices.GetService<IAuditLogger>();
                    if (audit != null)
                    {
                        var username = httpContext.User?.Identity?.Name ?? "unknown";
                        var action = approved ? "approval.grant" : "approval.deny";
                        var target = approval.ToolName ?? "unknown";
                        var details = JsonSerializer.Serialize(new { id, arguments = approval.Arguments, sessionId = approval.SessionId });
                        await audit.LogAdminActionAsync(username, action, target, details, true);
                    }

                    return Results.Ok(new { success = true });
                }
                return Results.NotFound(new { error = "Approval request not found." });
            });

            api.MapGet("/api/test/tools", async (string? serverId, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, [FromServices] SessionManager sessionManager, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                
                if (!string.IsNullOrWhiteSpace(serverId))
                {
                    servers = servers.Where(s => 
                        s.Id == serverId || 
                        (s.Categories != null && s.Categories.Contains(serverId))
                    ).ToList();
                }
                var allTools = new System.Collections.Generic.List<object>();
                var missingServers = new System.Collections.Generic.List<McpServer>();

                // Add custom tools from the registry
                foreach (var customTool in McpRouter.CustomTools.CustomToolRegistry.GetAll())
                {
                    bool includeTool = false;
                    if (customTool.Name.StartsWith("seerr_") && servers.Any(s => s.Id == "seerr"))
                        includeTool = true;
                    else if (customTool.Name.StartsWith("plex_") && servers.Any(s => s.Id == "plex"))
                        includeTool = true;

                    if (includeTool)
                    {
                        allTools.Add(new
                        {
                            name = customTool.Name,
                            description = customTool.Description,
                            inputSchema = customTool.InputSchema
                        });
                    }
                }

                foreach (var server in servers)
                {
                    if (server.Type == "custom") continue;
                    
                    var cached = sessionManager.GetServerToolsCache(server.Id);
                    if (cached != null)
                    {
                        logger.LogInformation("Server tools cache HIT for: {ServerId}", server.Id);
                        allTools.AddRange(cached);
                    }
                    else
                    {
                        logger.LogInformation("Server tools cache MISS for: {ServerId}", server.Id);
                        missingServers.Add(server);
                    }
                }

                if (missingServers.Count > 0)
                {
                    var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                    var tasks = missingServers.Where(s => s.Type != "custom").Select(async server =>
                    {
                        try
                        {
                            var conn = new BackendConnection(server, httpClient, logger, secretRetriever);
                            if (server.Type != "http" && server.Type != "streamable")
                            {
                                await conn.ConnectAsync();
                                conn.StartReader(msg => Task.CompletedTask);
                            }
                            var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                            await conn.SendRequestAsync("initialize", initReq);
                            await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                            backendConnections[server.Id] = conn;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to connect to server {ServerId} for tool listing", server.Id);
                        }
                    });
                    var allTasks = Task.WhenAll(tasks);
                    await Task.WhenAny(allTasks, Task.Delay(3000));

                    var routing = new Core.Routing.ToolRoutingManager();
                    await routing.PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"test-list\"}", backendConnections, logger, missingServers, sessionManager);

                    // Add tools from the newly fetched cache
                    foreach (var server in missingServers)
                    {
                        var newlyCached = sessionManager.GetServerToolsCache(server.Id);
                        if (newlyCached != null)
                        {
                            allTools.AddRange(newlyCached);
                        }
                    }

                    // Dispose backend connections after query
                    foreach (var conn in backendConnections.Values)
                    {
                        conn.Dispose();
                    }
                }

                return Results.Ok(allTools);
            });

            // 3. Test Call API
            api.MapPost("/api/test/call", async (
                [FromBody] TestCallModel model,
                [FromServices] IDbConnectionFactory dbFactory,
                [FromServices] HttpClient httpClient,
                [FromServices] IAuditLogger auditLogger,
                ILogger<Program> logger,
                HttpContext httpContext) =>
            {
                var username = httpContext.User?.Identity?.Name ?? "unknown";
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var dbConn = dbFactory.CreateConnection();
                var server = await dbConn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @Id", new { Id = model.ServerId });
                if (server == null && model.ServerId != "custom")
                {
                    var msg = $"Server {model.ServerId} not found";
                    await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments }), false, msg);
                    return Results.NotFound(msg);
                }

                // If executing a custom native tool
                var customTool = McpRouter.CustomTools.CustomToolRegistry.Get(model.ToolName);
                if (customTool != null)
                {
                    try
                    {
                        var args = model.Arguments.ValueKind == JsonValueKind.Undefined ? JsonDocument.Parse("{}").RootElement : model.Arguments;
                        var res = await customTool.ExecuteAsync(args, httpClient, dbFactory);

                        var details = JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments });
                        await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, details, true);

                        return Results.Ok(new {
                            content = new[] {
                                new { type = "text", text = JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }) }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        var details = JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments });
                        await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, details, false, ex.Message);
                        return Results.Problem(ex.Message);
                    }
                }

                if (server == null)
                {
                    var msg = "Server not found";
                    await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments }), false, msg);
                    return Results.NotFound();
                }

                try
                {
                    // Direct routing to backend
                    using var conn = new BackendConnection(server, httpClient, logger, secretRetriever);
                    if (server.Type != "http" && server.Type != "streamable")
                    {
                        await conn.ConnectAsync();
                        conn.StartReader(msg => Task.CompletedTask);
                    }

                    var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                    await conn.SendRequestAsync("initialize", initReq);
                    await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

                    var targetPayload = new
                    {
                        jsonrpc = "2.0",
                        id = "test-call-id",
                        method = "tools/call",
                        @params = new
                        {
                            name = model.ToolName,
                            arguments = model.Arguments
                        }
                    };
                    var targetBody = JsonSerializer.Serialize(targetPayload);
                    var result = await conn.SendRequestAsync("tools/call", targetBody);

                    var details = JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments });
                    await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, details, true);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    var details = JsonSerializer.Serialize(new { serverId = model.ServerId, arguments = model.Arguments });
                    await auditLogger.LogAdminActionAsync(username, "testbench.tools/call", model.ToolName, details, false, ex.Message);
                    return Results.Problem(ex.Message);
                }
            });

            // 4. Test Semantic Search API
            api.MapPost("/api/test/semantic-search", async ([FromBody] SearchModel model, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, [FromServices] IEmbeddingService embeddingService, [FromServices] SessionManager sessionManager, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                var allTools = new System.Collections.Generic.List<object>();
                var missingServers = new System.Collections.Generic.List<McpServer>();

                // Add custom tools
                foreach (var customTool in McpRouter.CustomTools.CustomToolRegistry.GetAll())
                {
                    bool includeTool = false;
                    if (customTool.Name.StartsWith("seerr_") && servers.Any(s => s.Id == "seerr"))
                        includeTool = true;
                    else if (customTool.Name.StartsWith("plex_") && servers.Any(s => s.Id == "plex"))
                        includeTool = true;

                    if (includeTool)
                    {
                        allTools.Add(new
                        {
                            name = customTool.Name,
                            description = customTool.Description,
                            inputSchema = customTool.InputSchema
                        });
                    }
                }

                foreach (var server in servers)
                {
                    if (server.Type == "custom") continue;
                    var cached = sessionManager.GetServerToolsCache(server.Id);
                    if (cached != null)
                    {
                        allTools.AddRange(cached);
                    }
                    else
                    {
                        missingServers.Add(server);
                    }
                }

                if (missingServers.Count > 0)
                {
                    var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                    var tasks = missingServers.Where(s => s.Type != "custom").Select(async server =>
                    {
                        try
                        {
                            var conn = new BackendConnection(server, httpClient, logger, secretRetriever);
                            if (server.Type != "http" && server.Type != "streamable")
                            {
                                await conn.ConnectAsync();
                                conn.StartReader(msg => Task.CompletedTask);
                            }
                            var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                            await conn.SendRequestAsync("initialize", initReq);
                            await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                            backendConnections[server.Id] = conn;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to connect to server {ServerId} for tool search", server.Id);
                        }
                    });
                    var allTasks = Task.WhenAll(tasks);
                    await Task.WhenAny(allTasks, Task.Delay(3000));

                    var routing = new Core.Routing.ToolRoutingManager();
                    await routing.PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"test-list\"}", backendConnections, logger, missingServers, sessionManager);

                    foreach (var server in missingServers)
                    {
                        var newlyCached = sessionManager.GetServerToolsCache(server.Id);
                        if (newlyCached != null)
                        {
                            allTools.AddRange(newlyCached);
                        }
                    }

                    foreach (var conn in backendConnections.Values)
                    {
                        conn.Dispose();
                    }
                }

                var scoredResults = await Core.Routing.SemanticSearchService.SearchToolsSemanticAsync(model.Query, allTools, embeddingService, logger);
                return Results.Ok(scoredResults);
            });

            // 2b. Test Prompts List API
            api.MapGet("/api/test/prompts", async (string? serverId, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, [FromServices] SessionManager sessionManager, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                
                if (!string.IsNullOrWhiteSpace(serverId))
                {
                    servers = servers.Where(s => 
                        s.Id == serverId || 
                        (s.Categories != null && s.Categories.Contains(serverId))
                    ).ToList();
                }
                var allPrompts = new System.Collections.Generic.List<object>();
                var missingServers = new System.Collections.Generic.List<McpServer>();

                foreach (var server in servers)
                {
                    if (server.Type == "custom") continue;
                    var cached = sessionManager.GetServerPromptsCache(server.Id);
                    if (cached != null)
                    {
                        allPrompts.AddRange(cached);
                    }
                    else
                    {
                        missingServers.Add(server);
                    }
                }

                if (missingServers.Count > 0)
                {
                    var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                    var tasks = missingServers.Where(s => s.Type != "custom").Select(async server =>
                    {
                        try
                        {
                            var conn = new BackendConnection(server, httpClient, logger, secretRetriever);
                            if (server.Type != "http" && server.Type != "streamable")
                            {
                                await conn.ConnectAsync();
                                conn.StartReader(msg => Task.CompletedTask);
                            }
                            var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                            await conn.SendRequestAsync("initialize", initReq);
                            await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                            backendConnections[server.Id] = conn;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to connect to server {ServerId} for prompt listing", server.Id);
                        }
                    });
                    var allTasks = Task.WhenAll(tasks);
                    await Task.WhenAny(allTasks, Task.Delay(3000));

                    var routing = new Core.Routing.PromptRoutingManager();
                    await routing.ListPromptsAsync("{\"jsonrpc\":\"2.0\",\"method\":\"prompts/list\",\"id\":\"test-list\"}", backendConnections, logger, () => Task.CompletedTask, sessionManager);

                    foreach (var server in missingServers)
                    {
                        var newlyCached = sessionManager.GetServerPromptsCache(server.Id);
                        if (newlyCached != null)
                        {
                            allPrompts.AddRange(newlyCached);
                        }
                    }

                    // Dispose backend connections after query
                    foreach (var conn in backendConnections.Values)
                    {
                        conn.Dispose();
                    }
                }

                // Append built-in meta-prompts
                allPrompts.Add(new Dictionary<string, object> {
                    { "name", "router__diagnose_failure" },
                    { "description", "[router] Diagnose an MCP tool execution failure and generate remediation suggestions." },
                    { "arguments", new[] {
                        new { name = "tool_name", description = "Name of the failing tool", required = true },
                        new { name = "error_message", description = "Exception message or error payload", required = true }
                    } }
                });
                allPrompts.Add(new Dictionary<string, object> {
                    { "name", "router__suggest_remix" },
                    { "description", "[router] Generate alternative tool call configurations or argument combinations based on current state." },
                    { "arguments", new[] {
                        new { name = "query", description = "User intent or goal description", required = true },
                        new { name = "failed_attempts", description = "Log of tried tool names and arguments", required = false }
                    } }
                });

                return Results.Ok(allPrompts);
            });

            // 2c. Test Resources List API
            api.MapGet("/api/test/resources", async (string? serverId, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, [FromServices] SessionManager sessionManager, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                
                if (!string.IsNullOrWhiteSpace(serverId))
                {
                    servers = servers.Where(s => 
                        s.Id == serverId || 
                        (s.Categories != null && s.Categories.Contains(serverId))
                    ).ToList();
                }
                var allResources = new System.Collections.Generic.List<object>();
                var allTemplates = new System.Collections.Generic.List<object>();
                var missingServers = new System.Collections.Generic.List<McpServer>();

                // Load custom file-based resources from data/resources
                var resourcesDir = Path.Combine(AppContext.BaseDirectory, "data", "resources");
                if (!Directory.Exists(resourcesDir))
                {
                    resourcesDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "resources");
                }
                if (Directory.Exists(resourcesDir))
                {
                    foreach (var file in Directory.GetFiles(resourcesDir))
                    {
                        try
                        {
                            var filename = Path.GetFileName(file);
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            var mimeType = "text/plain";
                            if (ext == ".md") mimeType = "text/markdown";
                            else if (ext == ".json") mimeType = "application/json";
                            else if (ext == ".html") mimeType = "text/html";

                            allResources.Add(new Dictionary<string, object> {
                                { "uri", "router://resources/" + filename },
                                { "name", "Local File: " + filename },
                                { "mimeType", mimeType },
                                { "description", "[custom] User-configured local resource file." }
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to load custom resource file {File}", file);
                        }
                    }
                }

                // Add built-in template
                allTemplates.Add(new Dictionary<string, object> {
                    { "uriTemplate", "logs://{server_name}/today" },
                    { "name", "Backend Server Log" },
                    { "description", "Fetch today's real-time logs for a specific backend server." },
                    { "parameters", new Dictionary<string, object> {
                        { "server_name", new Dictionary<string, object> {
                            { "description", "The unique identifier of the backend server (e.g., ha, unifi, docker)" }
                        } }
                    } }
                });

                foreach (var server in servers)
                {
                    if (server.Type == "custom") continue;
                    var cachedRes = sessionManager.GetServerResourcesCache(server.Id);
                    var cachedTemp = sessionManager.GetServerResourceTemplatesCache(server.Id);
                    if (cachedRes != null && cachedTemp != null)
                    {
                        allResources.AddRange(cachedRes);
                        allTemplates.AddRange(cachedTemp);
                    }
                    else
                    {
                        missingServers.Add(server);
                    }
                }

                if (missingServers.Count > 0)
                {
                    var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                    var tasks = missingServers.Where(s => s.Type != "custom").Select(async server =>
                    {
                        try
                        {
                            var conn = new BackendConnection(server, httpClient, logger, secretRetriever);
                            if (server.Type != "http" && server.Type != "streamable")
                            {
                                await conn.ConnectAsync();
                                conn.StartReader(msg => Task.CompletedTask);
                            }
                            var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                            await conn.SendRequestAsync("initialize", initReq);
                            await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                            backendConnections[server.Id] = conn;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to connect to server {ServerId} for resource listing", server.Id);
                        }
                    });
                    var allTasks = Task.WhenAll(tasks);
                    await Task.WhenAny(allTasks, Task.Delay(3000));

                    var routing = new Core.Routing.ResourceRoutingManager();
                    await routing.ListResourcesAsync("{\"jsonrpc\":\"2.0\",\"method\":\"resources/list\",\"id\":\"test-list\"}", backendConnections, logger, () => Task.CompletedTask, sessionManager);
                    await routing.ListResourceTemplatesAsync("{\"jsonrpc\":\"2.0\",\"method\":\"resources/templates/list\",\"id\":\"test-list\"}", backendConnections, logger, () => Task.CompletedTask, sessionManager);

                    foreach (var server in missingServers)
                    {
                        var newlyCachedRes = sessionManager.GetServerResourcesCache(server.Id);
                        var newlyCachedTemp = sessionManager.GetServerResourceTemplatesCache(server.Id);
                        if (newlyCachedRes != null) allResources.AddRange(newlyCachedRes);
                        if (newlyCachedTemp != null) allTemplates.AddRange(newlyCachedTemp);
                    }

                    // Dispose backend connections after query
                    foreach (var conn in backendConnections.Values)
                    {
                        conn.Dispose();
                    }
                }

                // Append built-in router resources at the end
                allResources.Add(new Dictionary<string, object> {
                    { "uri", "router://status" },
                    { "name", "Router Connection Status" },
                    { "mimeType", "application/json" },
                    { "description", "[router] View connection status and metadata for all backend MCP servers." }
                });
                allResources.Add(new Dictionary<string, object> {
                    { "uri", "router://config" },
                    { "name", "Active Configuration" },
                    { "mimeType", "application/json" },
                    { "description", "[router] View the router's active configuration database representation." }
                });

                return Results.Ok(new { resources = allResources, templates = allTemplates });
            });

            // 3b. Test Prompt Get API
            api.MapPost("/api/test/prompts/get", async ([FromBody] TestPromptGetModel model, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                var serverId = model.ServerId;
                if (serverId != "router" && !servers.Any(s => s.Id == serverId))
                {
                    return Results.NotFound($"Server {serverId} not found");
                }

                var routing = new Core.Routing.PromptRoutingManager();
                var promptName = model.PromptName;
                
                Func<string, string, string, string> rewriteRequestJson = (json, key, value) => {
                    try {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("params", out var paramsProp)) {
                            var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(paramsProp.GetRawText());
                            if (paramsDict != null) {
                                paramsDict[key] = JsonSerializer.SerializeToElement(value);
                                var newParams = JsonSerializer.Serialize(paramsDict);
                                var rootDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                                if (rootDict != null) {
                                    rootDict["params"] = JsonDocument.Parse(newParams).RootElement;
                                    return JsonSerializer.Serialize(rootDict);
                                }
                            }
                        }
                    } catch {}
                    return json;
                };

                var payload = new {
                    jsonrpc = "2.0",
                    id = "test-prompt-id",
                    method = "prompts/get",
                    @params = new {
                        name = promptName,
                        arguments = model.Arguments.ValueKind == JsonValueKind.Undefined ? (object)new Dictionary<string, object>() : model.Arguments
                    }
                };
                var body = JsonSerializer.Serialize(payload);

                if (serverId == "router")
                {
                    var res = await routing.GetPromptAsync(promptName, body, backendConnections, () => Task.CompletedTask, rewriteRequestJson);
                    return Results.Ok(res);
                }

                var targetServer = servers.First(s => s.Id == serverId);
                using var conn = new BackendConnection(targetServer, httpClient, logger, secretRetriever);
                if (targetServer.Type != "http" && targetServer.Type != "streamable")
                {
                    await conn.ConnectAsync();
                    conn.StartReader(msg => Task.CompletedTask);
                }
                var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                await conn.SendRequestAsync("initialize", initReq);
                await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                backendConnections[targetServer.Id] = conn;

                var promptRes = await routing.GetPromptAsync(promptName, body, backendConnections, () => Task.CompletedTask, rewriteRequestJson);
                return Results.Ok(promptRes);
            });

            // 3c. Test Resource Read API
            api.MapPost("/api/test/resources/read", async ([FromBody] TestResourceReadModel model, [FromServices] IDbConnectionFactory dbFactory, [FromServices] HttpClient httpClient, [FromServices] SessionManager sessionManager, ILogger<Program> logger, HttpContext httpContext) =>
            {
                var secretRetriever = httpContext.RequestServices.GetService<McpRouter.Core.Secrets.CompositeSecretRetriever>();
                using var connDb = dbFactory.CreateConnection();
                var rawServers = await connDb.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
                var servers = rawServers.ToList();
                var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                var uri = model.Uri;
                var routing = new Core.Routing.ResourceRoutingManager();
                Func<string, string, string, string> rewriteRequestJson = (json, key, value) => json;

                var payload = new {
                    jsonrpc = "2.0",
                    id = "test-resource-id",
                    method = "resources/read",
                    @params = new {
                        uri = uri
                    }
                };
                var body = JsonSerializer.Serialize(payload);

                if (uri.StartsWith("router://") || uri.StartsWith("logs://"))
                {
                    var localRes = await routing.ReadResourceAsync(uri, body, backendConnections, () => Task.CompletedTask, rewriteRequestJson, sessionManager);
                    return Results.Ok(localRes);
                }

                string serverId = "";
                if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) && parsedUri.Scheme == "mcp")
                {
                    serverId = parsedUri.Host;
                }

                if (string.IsNullOrEmpty(serverId) || !servers.Any(s => s.Id == serverId))
                {
                    return Results.BadRequest("Invalid resource URI or server not found");
                }

                var targetServer = servers.First(s => s.Id == serverId);
                using var conn = new BackendConnection(targetServer, httpClient, logger, secretRetriever);
                if (targetServer.Type != "http" && targetServer.Type != "streamable")
                {
                    await conn.ConnectAsync();
                    conn.StartReader(msg => Task.CompletedTask);
                }
                var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                await conn.SendRequestAsync("initialize", initReq);
                await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
                backendConnections[targetServer.Id] = conn;

                var res = await routing.ReadResourceAsync(uri, body, backendConnections, () => Task.CompletedTask, rewriteRequestJson, sessionManager);
                return Results.Ok(res);
            });

            // --- Custom Files Management APIs ---
            
            string SanitizeFileName(string name)
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                return new string(name.Where(c => !invalidChars.Contains(c) && c != '/' && c != '\\').ToArray());
            }

            string GetCustomFilesDirectory(string type)
            {
                string folder = type == "prompts" ? "prompts" : "resources";
                var path = Path.Combine(AppContext.BaseDirectory, "data", folder);
                if (!Directory.Exists(path))
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "data", folder);
                }
                Directory.CreateDirectory(path);
                return path;
            }

            api.MapGet("/api/custom-files", () =>
            {
                var result = new List<object>();
                try
                {
                    foreach (var type in new[] { "prompts", "resources" })
                    {
                        var dir = GetCustomFilesDirectory(type);
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            var info = new FileInfo(file);
                            result.Add(new
                            {
                                type = type,
                                name = info.Name,
                                sizeBytes = info.Length,
                                lastModified = info.LastWriteTimeUtc
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
                return Results.Ok(result);
            });

            api.MapGet("/api/custom-files/{type}/{name}", ([FromRoute] string type, [FromRoute] string name) =>
            {
                if (type != "prompts" && type != "resources") return Results.BadRequest("Invalid type");
                var cleanName = SanitizeFileName(name);
                if (string.IsNullOrEmpty(cleanName)) return Results.BadRequest("Invalid file name");

                var dir = GetCustomFilesDirectory(type);
                var filePath = Path.Combine(dir, cleanName);
                if (!File.Exists(filePath)) return Results.NotFound("File not found");

                try
                {
                    var text = File.ReadAllText(filePath);
                    return Results.Ok(new { content = text });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/api/custom-files/{type}/{name}", async ([FromRoute] string type, [FromRoute] string name, [FromBody] System.Text.Json.JsonElement body) =>
            {
                if (type != "prompts" && type != "resources") return Results.BadRequest("Invalid type");
                var cleanName = SanitizeFileName(name);
                if (string.IsNullOrEmpty(cleanName)) return Results.BadRequest("Invalid file name");

                if (type == "prompts" && !cleanName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanName += ".json";
                }

                if (!body.TryGetProperty("content", out var contentProp)) return Results.BadRequest("Missing content field");
                var content = contentProp.GetString() ?? "";

                if (type == "prompts")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest($"Invalid JSON format: {ex.Message}");
                    }
                }

                var dir = GetCustomFilesDirectory(type);
                var filePath = Path.Combine(dir, cleanName);

                try
                {
                    await File.WriteAllTextAsync(filePath, content);
                    return Results.Ok(new { success = true, name = cleanName });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            api.MapDelete("/api/custom-files/{type}/{name}", ([FromRoute] string type, [FromRoute] string name) =>
            {
                if (type != "prompts" && type != "resources") return Results.BadRequest("Invalid type");
                var cleanName = SanitizeFileName(name);
                if (string.IsNullOrEmpty(cleanName)) return Results.BadRequest("Invalid file name");

                var dir = GetCustomFilesDirectory(type);
                var filePath = Path.Combine(dir, cleanName);
                if (!File.Exists(filePath)) return Results.NotFound("File not found");

                try
                {
                    File.Delete(filePath);
                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            
        }
    }
}
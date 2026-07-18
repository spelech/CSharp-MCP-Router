using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureMcpRouterPipeline(this WebApplication app)
        {
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            
            // Request logging middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var headersString = string.Join(" | ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
                
                string bodyString = string.Empty;
                if (context.Request.ContentLength > 0)
                {
                    context.Request.EnableBuffering();
                    using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                    {
                        bodyString = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                    }
                }
            
                logger.LogInformation("Incoming request: {Method} {Path}{QueryString} from {Ip}. Body: {Body}. Headers: {Headers}", 
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Request.QueryString,
                    context.Connection.RemoteIpAddress,
                    bodyString,
                    headersString);
                await next();
            });
            
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                    ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                    ctx.Context.Response.Headers.Append("Expires", "0");
                }
            }); // Serves dashboard files from wwwroot with no-cache headers
            
            // Enforce SSO auth on dashboard APIs
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
                    !path.StartsWith("/api/register", StringComparison.OrdinalIgnoreCase) && 
                    !path.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase))
                {
                    var user = context.Request.Headers["Remote-User"].ToString();
                    if (string.IsNullOrEmpty(user))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: SSO session required." });
                        return;
                    }
                }
                await next();
            });
            
            app.SeedDatabase();
            
            // ----------------------------------------------------
            // SYSTEM/HEALTH ENDPOINTS
            // ----------------------------------------------------
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "McpRouter", version = "0.4.0" }));
            
            // ----------------------------------------------------
            // OAUTH & OIDC DISCOVERY ENDPOINTS
            // ----------------------------------------------------
            app.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    resource = $"{scheme}://{host}/mcp",
                    authorization_servers = new[] { $"{scheme}://{host}" },
                    bearer_methods_supported = new[] { "header" }
                });
            });
            
            app.MapGet("/.well-known/oauth-protected-resource/{**path}", (HttpContext context, string path) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    resource = $"{scheme}://{host}/{path}",
                    authorization_servers = new[] { $"{scheme}://{host}" },
                    bearer_methods_supported = new[] { "header" }
                });
            });
            
            app.MapGet("/.well-known/oauth-authorization-server", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    issuer = $"{scheme}://{host}",
                    authorization_endpoint = $"{scheme}://{host}/oauth/authorize",
                    token_endpoint = $"{scheme}://{host}/oauth/token",
                    registration_endpoint = $"{scheme}://{host}/api/register",
                    response_types_supported = new[] { "code" },
                    grant_types_supported = new[] { "authorization_code" },
                    token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" }
                });
            });
            
            app.MapGet("/.well-known/openid-configuration", (HttpContext context) =>
            {
                var host = context.Request.Host;
                var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : context.Request.Scheme;
                return Results.Json(new
                {
                    issuer = $"{scheme}://{host}",
                    authorization_endpoint = $"{scheme}://{host}/oauth/authorize",
                    token_endpoint = $"{scheme}://{host}/oauth/token",
                    registration_endpoint = $"{scheme}://{host}/api/register",
                    response_types_supported = new[] { "code" },
                    grant_types_supported = new[] { "authorization_code" },
                    subject_types_supported = new[] { "public" },
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                });
            });
            
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
            
                var sessionId = Guid.NewGuid().ToString("N");
                logger.LogInformation("New client SSE connection ({Method}). SessionId: {SessionId}", httpContext.Request.Method, sessionId);
            
                // Write SSE endpoint event
                // Directs the client to POST messages to the absolute URL
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrEmpty(scheme)) scheme = httpContext.Request.Scheme;
                var host = httpContext.Request.Host.Value;
                var absoluteUrl = $"{scheme}://{host}/message?sessionId={sessionId}";
                await httpContext.Response.WriteAsync($"event: endpoint\ndata: {absoluteUrl}\n\n");
                await httpContext.Response.Body.FlushAsync();
            
                bool metaMode = httpContext.Request.Query["meta"] != "false";
                // Create session and initialize connections to backend servers
                var session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response, targetServerId: null, metaMode);
            
                // Read body if POST
                if (httpContext.Request.Method == "POST")
                {
                    try
                    {
                        httpContext.Request.EnableBuffering();
                        string requestBody;
                        using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                            httpContext.Request.Body.Position = 0;
                        }
            
                        if (!string.IsNullOrEmpty(requestBody))
                        {
                            logger.LogInformation("Processing initial JSON-RPC message in POST /sse body: {Body}", requestBody);
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("method", out var methodProp))
                            {
                                var method = methodProp.GetString() ?? string.Empty;
                                var id = root.TryGetProperty("id", out var idProp) ? idProp.Clone() : (JsonElement?)null;
                                
                                if (method == "initialize")
                                {
                                    var response = new
                                    {
                                        jsonrpc = "2.0",
                                        id = id != null ? (object)id : null,
                                        result = new
                                        {
                                            protocolVersion = "2024-11-05",
                                            capabilities = new { tools = new { listChanged = true } },
                                            serverInfo = new { name = "McpRouterGateway", version = "0.4.0" }
                                        }
                                    };
                                    await session.WriteMessageAsync(response);
                                    session.StartInitialization(requestBody);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse initial POST message body for SessionId: {SessionId}", sessionId);
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
                    sessionManager.CloseSession(sessionId);
                }
            });
            
            // Minimal API route for handling GET (SSE initialization) and POST (JSON-RPC requests)
            app.MapMethods("/{targetServerId:regex(^[a-zA-Z0-9_-]+$)}", new[] { "GET", "POST", "HEAD" }, async (HttpContext httpContext, [FromServices] SessionManager sessionManager, [FromServices] RouterDbContext db, ILogger<Program> logger, string targetServerId) =>
            {
                var isSse = httpContext.Request.Headers.Accept.ToString().Contains("text/event-stream");
                var isPost = HttpMethods.IsPost(httpContext.Request.Method);
                bool metaMode = httpContext.Request.Query["meta"] == "true";
            
                // Ensure session ID is tracked (using Bearer token or fallback to new Guid)
                bool hasBearerToken = false;
                string sessionId;
                if (httpContext.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    sessionId = httpContext.Request.Headers.Authorization.ToString().Substring("Bearer ".Length).Trim();
                    hasBearerToken = true;
                }
                else
                {
                    sessionId = Guid.NewGuid().ToString("N");
                }
            
                if (!isPost && (isSse || HttpMethods.IsGet(httpContext.Request.Method)))
                {
                    // ---------------------------------------------------------
                    // Handle SSE GET Request (Initialize connection)
                    // ---------------------------------------------------------
                    logger.LogInformation("New SSE connection established. Session ID: {SessionId}. Target Server: {TargetServerId}", sessionId, targetServerId ?? "ALL");
            
                    httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
                    httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                    httpContext.Response.Headers.Append("Connection", "keep-alive");
                    await httpContext.Response.Body.FlushAsync();
            
                    var newSession = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response, targetServerId, metaMode);
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
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse POST message body for /mcp");
                    }
                }
            
                // If POST and NOT initialize, and we have a bearer token, route to existing session
                if (httpContext.Request.Method == "POST" && method != "initialize" && hasBearerToken)
                {
                    var activeSession = sessionManager.GetSession(sessionId);
                    if (activeSession == null)
                    {
                        logger.LogWarning("Active session not found for session ID: {SessionId}", sessionId);
                        httpContext.Response.StatusCode = 404;
                        await httpContext.Response.WriteAsJsonAsync(new { error = "Session not found." });
                        return;
                    }
            
                    logger.LogInformation("Routing POST request method {Method} to active session: {SessionId}", method, sessionId);
                    
                    try
                    {
                        if (method == "tools/list")
                        {
                            var tools = await activeSession.ListToolsAsync(requestBody);
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
                            using var doc = JsonDocument.Parse(requestBody);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("name", out var nameProp))
                            {
                                var toolName = nameProp.GetString() ?? string.Empty;
                                var res = await activeSession.CallToolAsync(toolName, requestBody, db);
                                
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
                            await httpContext.Response.WriteAsJsonAsync(new { error = "Invalid tools/call: missing name parameter" });
                            return;
                        }
                        else
                        {
                            // General broadcast for notifications or other standard requests
                            var results = await activeSession.BroadcastRequestAsync(requestBody);
                            if (results.Count > 0 && id != null)
                            {
                                var response = new
                                {
                                    jsonrpc = "2.0",
                                    id = (object)id,
                                    result = results.First().Value
                                };
                                httpContext.Response.Headers.ContentType = "application/json";
                                await httpContext.Response.WriteAsJsonAsync(response);
                                return;
                            }
                            
                            httpContext.Response.StatusCode = 202; // Accepted
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing routed message for session {SessionId}", sessionId);
                        httpContext.Response.StatusCode = 500;
                        await httpContext.Response.WriteAsJsonAsync(new { error = ex.Message });
                        return;
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
            
                if (httpContext.Request.Method == "POST" && method == "initialize")
                {
                    try
                    {
                        logger.LogInformation("Processing initial JSON-RPC message in POST /mcp body: {Body}", requestBody);
                        var serverName = "McpRouterGateway";
                        if (!string.IsNullOrWhiteSpace(targetServerId))
                        {
                            var targetServer = await db.Servers.FirstOrDefaultAsync(s => s.Id == targetServerId);
                            if (targetServer != null)
                            {
                                serverName = targetServer.DisplayName;
                            }
                            else if (await db.Servers.AnyAsync(s => s.Category == targetServerId))
                            {
                                // Fallback to Category name
                                serverName = char.ToUpper(targetServerId[0]) + targetServerId.Substring(1) + " Services";
                            }
                        }
            
                        var response = new
                        {
                            jsonrpc = "2.0",
                            id = id != null ? (object)id : null,
                            result = new
                            {
                                protocolVersion = "2024-11-05",
                                capabilities = new { tools = new { listChanged = true } },
                                serverInfo = new { name = serverName, version = "0.4.0" }
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
            });
            
            // ----------------------------------------------------
            // MCP CLIENT MESSAGE ROUTER
            // ----------------------------------------------------
            var handleMessage = async (HttpContext httpContext, string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                var session = sessionManager.GetSession(sessionId);
                if (session == null)
                {
                    return Results.NotFound(new { error = "Session not found." });
                }
            
                using var reader = new StreamReader(httpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                
                logger.LogDebug("Received JSON-RPC message for Session {SessionId}: {Body}", sessionId, body);
            
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    
                    if (!root.TryGetProperty("method", out var methodProp))
                    {
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
                                    tools = new { listChanged = true }
                                },
                                serverInfo = new
                                {
                                    name = "McpRouterGateway",
                                    version = "0.4.0"
                                }
                            }
                        };
                        
                        // Write SSE event to client stream
                        await session.WriteMessageAsync(response);
                        
                        session.StartInitialization(body);
                        
                        return Results.Accepted();
                    }
                    else if (method == "tools/list")
                    {
                        var tools = await session.ListToolsAsync(body);
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
                            var db = httpContext.RequestServices.GetRequiredService<RouterDbContext>();
                            var res = await session.CallToolAsync(toolName, body, db);
                            
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
                        var resources = await session.ListResourcesAsync(body);
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
                            var res = await session.ReadResourceAsync(uri, body);
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
                    else if (method == "prompts/list")
                    {
                        var prompts = await session.ListPromptsAsync(body);
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
                            var res = await session.GetPromptAsync(name, body);
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
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error routing client message.");
                    return Results.Problem(ex.Message);
                }
            };
            
            app.MapPost("/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) => 
                await handleMessage(httpContext, sessionId, sessionManager, logger));
            
            app.MapPost("/mcp/message", async (HttpContext httpContext, [FromQuery] string sessionId, [FromServices] SessionManager sessionManager, ILogger<Program> logger) => 
                await handleMessage(httpContext, sessionId, sessionManager, logger));
            
            // ----------------------------------------------------
            // DCR & OAUTH ENDPOINTS
            // ----------------------------------------------------
            
            app.MapGet("/api/me", (HttpContext context) =>
            {
                var user = context.Request.Headers["Remote-User"].ToString();
                var name = context.Request.Headers["Remote-Name"].ToString();
                var email = context.Request.Headers["Remote-Email"].ToString();
                var groups = context.Request.Headers["Remote-Groups"].ToString();
            
                if (string.IsNullOrEmpty(user))
                {
                    return Results.Ok(new { authenticated = false });
                }
            
                return Results.Ok(new
                {
                    authenticated = true,
                    username = user,
                    name = string.IsNullOrEmpty(name) ? user : name,
                    email = email,
                    groups = groups.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                });
            });
            
            
            
            // ----------------------------------------------------
            // DASHBOARD MANAGEMENT ENDPOINTS
            // ----------------------------------------------------
            app.MapGet("/api/servers", async ([FromServices] RouterDbContext db) =>
            {
                var servers = await db.Servers.ToListAsync();
                // Do not return actual keys to client
                var sanitized = servers.Select(s => new {
                    s.Id,
                    s.DisplayName,
                    s.Url,
                    s.Enabled,
                    s.Hidden,
                    s.Type,
                    s.Category,
                    s.HeadersJson,
                    HasApiKey = !string.IsNullOrEmpty(s.ApiKey)
                });
                return Results.Ok(sanitized);
            });
            
            app.MapPut("/api/servers/{id}", async (string id, [FromBody] McpServer update, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager) =>
            {
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
                if (server == null)
                {
                    return Results.NotFound();
                }
            
                server.Enabled = update.Enabled;
                server.Hidden = update.Hidden;
                
                if (!string.IsNullOrEmpty(update.DisplayName))
                {
                    server.DisplayName = update.DisplayName;
                }
                if (!string.IsNullOrEmpty(update.Url))
                {
                    server.Url = update.Url;
                }
                if (!string.IsNullOrEmpty(update.Type))
                {
                    server.Type = update.Type;
                }
                if (!string.IsNullOrEmpty(update.Category))
                {
                    server.Category = update.Category;
                }
                if (update.ApiKey != null)
                {
                    server.ApiKey = update.ApiKey;
                }
                if (update.HeadersJson != null)
                {
                    server.HeadersJson = update.HeadersJson;
                }
                
                await db.SaveChangesAsync();
                
                // Reset active sessions so they reconnect to updated backends
                sessionManager.ResetAll();
            
                return Results.Ok(server);
            });
            
            app.MapPost("/api/servers", async ([FromBody] McpServer server, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager) =>
            {
                if (string.IsNullOrEmpty(server.Id))
                {
                    server.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                }
                
                db.Servers.Add(server);
                await db.SaveChangesAsync();
                
                sessionManager.ResetAll();
                return Results.Ok(server);
            });
            
            app.MapDelete("/api/servers/{id}", async (string id, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager) =>
            {
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
                if (server == null)
                {
                    return Results.NotFound();
                }
                
                db.Servers.Remove(server);
                await db.SaveChangesAsync();
                
                sessionManager.ResetAll();
                return Results.Ok(new { success = true });
            });

            // --- TEST BENCH & LOGS ENDPOINTS ---

            // 1. Logs API
            app.MapGet("/api/logs", () => Results.Ok(LogBuffer.GetLogs()));
            app.MapDelete("/api/logs", () => {
                LogBuffer.Clear();
                return Results.Ok(new { success = true });
            });

            // 1.5. Settings API
            app.MapGet("/api/settings", (DynamicEmbeddingService embeddingService) => 
                Results.Ok(embeddingService.GetSettings()));
                
            app.MapPost("/api/settings", (RouterSettings settings, DynamicEmbeddingService embeddingService) => {
                embeddingService.SaveSettings(settings);
                return Results.Ok(new { success = true, settings = embeddingService.GetSettings() });
            });

            // 2. Test Tools List API
            app.MapGet("/api/test/tools", async ([FromServices] RouterDbContext db, [FromServices] HttpClient httpClient, ILogger<Program> logger) =>
            {
                var servers = await db.Servers.Where(s => s.Enabled).ToListAsync();
                var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                var tasks = servers.Where(s => s.Type != "custom").Select(async server =>
                {
                    try
                    {
                        var conn = new BackendConnection(server, httpClient, logger);
                        if (server.Type != "http" && server.Type != "streamable")
                        {
                            await conn.ConnectAsync();
                        }
                        var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                        await conn.SendRequestAsync("initialize", initReq);
                        backendConnections[server.Id] = conn;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to server {ServerId} for tool listing", server.Id);
                    }
                });
                await Task.WhenAll(tasks);

                var routing = new Core.Routing.ToolRoutingManager();
                await routing.PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"test-list\"}", backendConnections, logger, servers);

                // Dispose backend connections after query
                foreach (var conn in backendConnections.Values)
                {
                    conn.Dispose();
                }

                return Results.Ok(routing.GetCachedTools());
            });

            // 3. Test Call API
            app.MapPost("/api/test/call", async ([FromBody] TestCallModel model, [FromServices] RouterDbContext db, [FromServices] HttpClient httpClient, ILogger<Program> logger) =>
            {
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == model.ServerId);
                if (server == null && model.ServerId != "custom")
                {
                    return Results.NotFound($"Server {model.ServerId} not found");
                }

                // If executing a custom native tool
                var customTool = McpRouter.CustomTools.CustomToolRegistry.Get(model.ToolName);
                if (customTool != null)
                {
                    try
                    {
                        var args = model.Arguments.ValueKind == JsonValueKind.Undefined ? JsonDocument.Parse("{}").RootElement : model.Arguments;
                        var res = await customTool.ExecuteAsync(args, httpClient, db);
                        return Results.Ok(new {
                            content = new[] {
                                new { type = "text", text = JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }) }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                }

                if (server == null) return Results.NotFound();

                // Direct routing to backend
                using var conn = new BackendConnection(server, httpClient, logger);
                if (server.Type != "http" && server.Type != "streamable")
                {
                    await conn.ConnectAsync();
                }
                
                var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                await conn.SendRequestAsync("initialize", initReq);

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
                return Results.Ok(result);
            });

            // 4. Test Semantic Search API
            app.MapPost("/api/test/semantic-search", async ([FromBody] SearchModel model, [FromServices] RouterDbContext db, [FromServices] HttpClient httpClient, [FromServices] IEmbeddingService embeddingService, ILogger<Program> logger) =>
            {
                var servers = await db.Servers.Where(s => s.Enabled).ToListAsync();
                var backendConnections = new System.Collections.Concurrent.ConcurrentDictionary<string, BackendConnection>();

                var tasks = servers.Where(s => s.Type != "custom").Select(async server =>
                {
                    try
                    {
                        var conn = new BackendConnection(server, httpClient, logger);
                        if (server.Type != "http" && server.Type != "streamable")
                        {
                            await conn.ConnectAsync();
                        }
                        var initReq = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"test-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpTestBench\",\"version\":\"0.4.0\"}}}";
                        await conn.SendRequestAsync("initialize", initReq);
                        backendConnections[server.Id] = conn;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to server {ServerId} for tool search", server.Id);
                    }
                });
                await Task.WhenAll(tasks);

                var routing = new Core.Routing.ToolRoutingManager();
                await routing.PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"test-list\"}", backendConnections, logger, servers);

                foreach (var conn in backendConnections.Values)
                {
                    conn.Dispose();
                }

                var tools = routing.GetCachedTools();
                var scoredResults = await Core.Routing.SemanticSearchService.SearchToolsSemanticAsync(model.Query, tools, embeddingService);
                return Results.Ok(scoredResults);
            });

            app.Run();
            
        }
    }

    public class TestCallModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class SearchModel
    {
        public string Query { get; set; } = string.Empty;
    }
}

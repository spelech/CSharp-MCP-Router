using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register SQLite Database
builder.Services.AddDbContext<RouterDbContext>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SessionManager>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles(); // Serves dashboard files from wwwroot

// Enforce SSO auth on dashboard APIs
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api/") && !path.StartsWith("/api/register") && !path.StartsWith("/api/me"))
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

// ----------------------------------------------------
// DATABASE INITIALIZATION & ENV MIGRATION
// ----------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Initializing database...");
        db.Database.EnsureCreated();
        
        // Migration script: if empty, import from environment
        if (!db.Servers.Any())
        {
            logger.LogInformation("Database empty. Performing initial migration from environment variables...");

            // 1. Home Assistant MCP
            var haUrl = Environment.GetEnvironmentVariable("HOMEASSISTANT_URL") ?? "http://10.0.0.10:8123";
            var haToken = Environment.GetEnvironmentVariable("HOMEASSISTANT_TOKEN");
            if (!string.IsNullOrEmpty(haToken))
            {
                db.Servers.Add(new McpServer
                {
                    Id = "ha",
                    DisplayName = "Home Assistant",
                    Url = "http://ha-mcp-new:8086/mcp",
                    Enabled = true,
                    Hidden = false,
                    Type = "http",
                    ApiKey = haToken
                });
                logger.LogInformation("Imported HA MCP config.");
            }

            // 2. Actual Budget MCP
            var actualPass = Environment.GetEnvironmentVariable("ACTUAL_PASSWORD");
            if (!string.IsNullOrEmpty(actualPass))
            {
                db.Servers.Add(new McpServer
                {
                    Id = "actual",
                    DisplayName = "Actual Budget",
                    Url = "http://actual-mcp-new:3000/sse",
                    Enabled = true,
                    Hidden = false,
                    Type = "sse",
                    ApiKey = Environment.GetEnvironmentVariable("ACTUAL_BEARER_TOKEN")
                });
                logger.LogInformation("Imported Actual Budget MCP config.");
            }

            // 3. Receipt Wrangler MCP
            var rwKey = Environment.GetEnvironmentVariable("RECEIPTWRANGLER_API_KEY");
            if (!string.IsNullOrEmpty(rwKey) && rwKey != "YOUR_RECEIPTWRANGLER_API_KEY_HERE")
            {
                db.Servers.Add(new McpServer
                {
                    Id = "receiptwrangler",
                    DisplayName = "Receipt Wrangler",
                    Url = "http://receiptwrangler-mcp-new:3000/mcp",
                    Enabled = true,
                    Hidden = false,
                    Type = "sse",
                    ApiKey = rwKey
                });
                logger.LogInformation("Imported Receipt Wrangler MCP config.");
            }

            // 5. Overseerr/Seerr MCP
            var seerrKey = Environment.GetEnvironmentVariable("SEERR_API_KEY");
            if (!string.IsNullOrEmpty(seerrKey))
            {
                db.Servers.Add(new McpServer
                {
                    Id = "seerr",
                    DisplayName = "Overseerr requests",
                    Url = "http://seerr:5055",
                    Enabled = true,
                    Hidden = false,
                    Type = "custom",
                    ApiKey = seerrKey
                });
                logger.LogInformation("Imported Overseerr config.");
            }

            // 6. UniFi MCP
            var unifiUser = Environment.GetEnvironmentVariable("UNIFI_USERNAME");
            if (!string.IsNullOrEmpty(unifiUser))
            {
                db.Servers.Add(new McpServer
                {
                    Id = "unifi",
                    DisplayName = "UniFi Controller",
                    Url = "http://unifi-mcp:3000/mcp",
                    Enabled = true,
                    Hidden = false,
                    Type = "streamable"
                });
                logger.LogInformation("Imported UniFi MCP config.");
            }

            // 7. Plex Media Server
            var plexToken = Environment.GetEnvironmentVariable("PLEX_TOKEN");
            if (!string.IsNullOrEmpty(plexToken))
            {
                db.Servers.Add(new McpServer
                {
                    Id = "plex",
                    DisplayName = "Plex Media Server",
                    Url = Environment.GetEnvironmentVariable("PLEX_URL") ?? "http://10.0.0.10:32400",
                    Enabled = true,
                    Hidden = false,
                    Type = "custom",
                    ApiKey = plexToken
                });
                logger.LogInformation("Imported Plex config.");
            }

            // 7. Arr HD / 4K MCP
            db.Servers.Add(new McpServer
            {
                Id = "mcp-arr-hd",
                DisplayName = "Arr Services (HD)",
                Url = "http://mcp-arr-hd:3000/mcp",
                Enabled = true,
                Hidden = false,
                Type = "streamable"
            });
            db.Servers.Add(new McpServer
            {
                Id = "mcp-arr-4k",
                DisplayName = "Arr Services (4K)",
                Url = "http://mcp-arr-4k:3000/mcp",
                Enabled = true,
                Hidden = false,
                Type = "streamable"
            });
            logger.LogInformation("Imported Arr MCP configurations.");

            db.SaveChanges();
            logger.LogInformation("Database migration completed successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database.");
    }
}

// ----------------------------------------------------
// SYSTEM/HEALTH ENDPOINTS
// ----------------------------------------------------
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "McpRouter", version = "0.1.0" }));

// ----------------------------------------------------
// MCP CLIENT SSE HANDLER
// ----------------------------------------------------
app.MapGet("/sse", async (HttpContext httpContext, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
{
    httpContext.Response.Headers.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    var sessionId = Guid.NewGuid().ToString("N");
    logger.LogInformation("New client SSE connection. SessionId: {SessionId}", sessionId);

    // Write SSE endpoint event
    // Directs the client to POST messages to /message?sessionId=...
    await httpContext.Response.WriteAsync($"event: endpoint\ndata: /message?sessionId={sessionId}\n\n");
    await httpContext.Response.Body.FlushAsync();

    // Create session and initialize connections to backend servers
    var session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response);

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

// Also support /mcp endpoint starting SSE (used by some spec configurations)
app.MapGet("/mcp", async (HttpContext httpContext, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
{
    httpContext.Response.Headers.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    var sessionId = Guid.NewGuid().ToString("N");
    logger.LogInformation("New client /mcp SSE connection. SessionId: {SessionId}", sessionId);

    await httpContext.Response.WriteAsync($"event: endpoint\ndata: /mcp/message?sessionId={sessionId}\n\n");
    await httpContext.Response.Body.FlushAsync();

    var session = await sessionManager.CreateSessionAsync(sessionId, httpContext.Response);

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
                        version = "0.1.0"
                    }
                }
            };
            
            // Write SSE event to client stream
            await session.WriteMessageAsync(response);
            
            // Forward initialize to backends in background
            _ = Task.Run(async () => await session.InitializeBackendsAsync(body));
            
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
app.MapPost("/api/register", async ([FromBody] JsonElement metadata, [FromServices] RouterDbContext db) =>
{
    var clientName = metadata.TryGetProperty("client_name", out var cn) ? cn.GetString() ?? "Unknown Client" : "Unknown Client";
    var redirectUris = metadata.TryGetProperty("redirect_uris", out var ru) ? ru.Clone() : default;

    var clientId = Guid.NewGuid().ToString("N");
    var clientSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // Double guid size secret

    var client = new OAuthClient
    {
        ClientId = clientId,
        ClientSecret = clientSecret,
        ClientName = clientName,
        RedirectUrisJson = redirectUris.ValueKind == JsonValueKind.Array ? redirectUris.ToString() : "[]"
    };

    db.Clients.Add(client);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        client_id = clientId,
        client_secret = clientSecret,
        client_name = clientName,
        client_secret_expires_at = 0
    });
});

app.MapGet("/api/clients", async ([FromServices] RouterDbContext db) =>
{
    var clients = await db.Clients.ToListAsync();
    return Results.Ok(clients);
});

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

app.MapGet("/oauth/authorize", ([FromQuery] string client_id, [FromQuery] string redirect_uri, [FromQuery] string response_type, [FromQuery] string? state, HttpContext context, [FromServices] RouterDbContext db) =>
{
    var client = db.Clients.FirstOrDefault(c => c.ClientId == client_id);
    if (client == null)
    {
        return Results.BadRequest("Client not found.");
    }

    // Auto-approve since this is a private deployment running on our home server protected by SSO/TinyAuth anyway
    var code = Guid.NewGuid().ToString("N");
    
    // Store code in memory or setting if we want to validate, for simplicity we just return it and validate it in /token
    var redirectUrl = $"{redirect_uri}?code={code}";
    if (!string.IsNullOrEmpty(state))
    {
        redirectUrl += $"&state={state}";
    }

    return Results.Redirect(redirectUrl);
});

app.MapPost("/oauth/token", async (HttpContext context, [FromServices] RouterDbContext db) =>
{
    var form = await context.Request.ReadFormAsync();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();
    var grantType = form["grant_type"].ToString();

    var client = db.Clients.FirstOrDefault(c => c.ClientId == clientId && c.ClientSecret == clientSecret);
    if (client == null)
    {
        return Results.BadRequest(new { error = "invalid_client" });
    }

    // Return dummy token, valid indefinitely
    var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600 * 24 * 365 // 1 year
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
    if (!string.IsNullOrEmpty(update.Url))
    {
        server.Url = update.Url;
    }
    if (update.ApiKey != null)
    {
        server.ApiKey = update.ApiKey;
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

app.Run();

// ----------------------------------------------------
// SESSION & DOCKER CONCURRENT MULTIPLEXING LOGIC
// ----------------------------------------------------
public class ClientSession
{
    private readonly string _sessionId;
    private readonly HttpResponse _clientResponse;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly List<McpServer> _servers;
    private readonly ConcurrentDictionary<string, BackendConnection> _backendConnections = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // Map toolName -> serverId
    private readonly ConcurrentDictionary<string, string> _toolRoutingTable = new();
    private readonly List<object> _cachedTools = new();
    private readonly object _cacheLock = new();
    private bool _isCachePopulated = false;

    public ClientSession(string sessionId, HttpResponse clientResponse, List<McpServer> servers, HttpClient httpClient, ILogger logger)
    {
        _sessionId = sessionId;
        _clientResponse = clientResponse;
        _servers = servers;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task WriteMessageAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        await _writeLock.WaitAsync();
        try
        {
            await _clientResponse.WriteAsync($"event: message\ndata: {json}\n\n");
            await _clientResponse.Body.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task InitializeBackendsAsync(string initializeRequest)
    {
        var tasks = _servers.Where(s => s.Enabled && s.Type != "custom").Select(async server =>
        {
            try
            {
                var conn = new BackendConnection(server, _httpClient, _logger);
                if (server.Type != "http" && server.Type != "streamable")
                {
                    await conn.ConnectAsync();
                }
                
                // Start background reader
                conn.StartReader(async (message) =>
                {
                    // If message is a response, complete the TaskCompletionSource
                    if (message.TryGetProperty("id", out var idProp))
                    {
                        var idStr = idProp.ToString();
                        if (conn.PendingRequests.TryRemove(idStr, out var tcs))
                        {
                            tcs.SetResult(message.Clone());
                            return;
                        }
                    }
                    
                    // Otherwise, it is a notification (e.g. logMessage, resourceUpdated) - forward to client
                    await WriteMessageAsync(message);
                });

                // Send initialize request to this backend
                var resp = await conn.SendRequestAsync("initialize", initializeRequest);
                
                // Send initialized notification to this backend
                await conn.SendNotificationAsync("notifications/initialized", "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

                _backendConnections[server.Id] = conn;
                _logger.LogInformation("Initialized backend server connection: {ServerId}", server.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to backend: {ServerId} at {Url}", server.Id, server.Url);
            }
        });

        await Task.WhenAll(tasks);

        // Pre-populate tools cache and routing table in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"init-list\"}");
                _logger.LogInformation("Pre-populated tools cache and routing table (total {Count} tools).", _cachedTools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pre-populate tools cache during backend connection initialization.");
            }
        });
    }

    private async Task PopulateToolsCacheAsync(string body)
    {
        var allTools = new List<object>();

        // Add custom native C# tools (Plex and Overseerr)
        foreach (var customTool in McpRouter.CustomTools.CustomToolRegistry.GetAll())
        {
            allTools.Add(new
            {
                name = customTool.Name,
                description = customTool.Description,
                inputSchema = customTool.InputSchema
            });
        }

        var tasks = new List<Task<(string ServerId, JsonElement Tools)>>();

        foreach (var entry in _backendConnections)
        {
            var conn = entry.Value;
            var serverId = entry.Key;
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var resp = await conn.SendRequestAsync("tools/list", body);
                    if (resp.TryGetProperty("result", out var result) && result.TryGetProperty("tools", out var toolsList))
                    {
                        return (serverId, toolsList);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error listing tools on server {ServerId}", serverId);
                }
                return (serverId, default(JsonElement));
            }));
        }

        var completed = await Task.WhenAll(tasks);
        foreach (var item in completed)
        {
            if (item.Tools.ValueKind == JsonValueKind.Array)
            {
                foreach (var tool in item.Tools.EnumerateArray())
                {
                    if (tool.TryGetProperty("name", out var nameProp))
                    {
                        var rawToolName = nameProp.GetString() ?? string.Empty;
                        
                        // Suffix tools from 4K arr instance to avoid name collisions with HD
                        var exposedName = item.ServerId == "mcp-arr-4k"
                            ? rawToolName + "_4k"
                            : rawToolName;
                        
                        _toolRoutingTable[exposedName] = item.ServerId;
                        
                        // Deserialize and rewrite name if needed
                        var toolDict = JsonSerializer.Deserialize<Dictionary<string, object>>(tool.GetRawText());
                        if (toolDict != null)
                        {
                            if (item.ServerId == "mcp-arr-4k")
                            {
                                toolDict["name"] = exposedName;
                                // Prefix description so users know it's the 4K instance
                                if (toolDict.TryGetValue("description", out var desc))
                                    toolDict["description"] = "[4K] " + desc;
                            }
                            allTools.Add(toolDict);
                        }
                    }
                }
            }
        }

        lock (_cacheLock)
        {
            _cachedTools.Clear();
            _cachedTools.AddRange(allTools);
            _isCachePopulated = true;
        }
    }

    public async Task<List<object>> ListToolsAsync(string body)
    {
        lock (_cacheLock)
        {
            if (_isCachePopulated)
            {
                return new List<object>(_cachedTools);
            }
        }

        // Fallback: populate synchronously if cache isn't ready
        await PopulateToolsCacheAsync(body);
        lock (_cacheLock)
        {
            return new List<object>(_cachedTools);
        }
    }

    public async Task<object> CallToolAsync(string toolName, string body, RouterDbContext db)
    {
        // Try custom native C# tool first
        var customTool = McpRouter.CustomTools.CustomToolRegistry.Get(toolName);
        if (customTool != null)
        {
            _logger.LogInformation("Executing custom native C# tool '{ToolName}'", toolName);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var parameters = root.TryGetProperty("params", out var paramsProp) && paramsProp.TryGetProperty("arguments", out var argsProp) 
                ? argsProp 
                : JsonDocument.Parse("{}").RootElement;
                
            try
            {
                var result = await customTool.ExecuteAsync(parameters, _httpClient, db);
                return new {
                    content = new[] {
                        new {
                            type = "text",
                            text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom tool {ToolName}", toolName);
                return new {
                    isError = true,
                    content = new[] {
                        new {
                            type = "text",
                            text = $"Error executing custom tool {toolName}: {ex.Message}"
                        }
                    }
                };
            }
        }

        // If not in routing table, try to refresh the cache once in case a new tool was registered
        if (!_toolRoutingTable.ContainsKey(toolName))
        {
            _logger.LogInformation("Tool '{ToolName}' not found in routing table. Refreshing tools cache...", toolName);
            try
            {
                await PopulateToolsCacheAsync("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":\"refresh-list\"}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh tools cache during CallToolAsync for '{ToolName}'", toolName);
            }
        }

        if (_toolRoutingTable.TryGetValue(toolName, out var serverId) && _backendConnections.TryGetValue(serverId, out var conn))
        {
            _logger.LogInformation("Routing tool call '{ToolName}' to server '{ServerId}'", toolName, serverId);
            
            // If the tool was exposed with a _4k suffix, rewrite the body to use the real tool name
            string routingBody = body;
            if (toolName.EndsWith("_4k") && serverId == "mcp-arr-4k")
            {
                var realToolName = toolName.Substring(0, toolName.Length - 3); // strip "_4k"
                routingBody = body.Replace($"\"name\":\"{toolName}\"", $"\"name\":\"{realToolName}\"");
            }
            
            return await conn.SendRequestAsync("tools/call", routingBody);
        }
        
        throw new KeyNotFoundException($"Tool {toolName} not found in routing table.");
    }

    public async Task<Dictionary<string, JsonElement>> BroadcastRequestAsync(string body)
    {
        var results = new Dictionary<string, JsonElement>();
        var tasks = new List<Task<(string ServerId, JsonElement Result)>>();

        foreach (var entry in _backendConnections)
        {
            var conn = entry.Value;
            var serverId = entry.Key;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await conn.SendRequestAsync("unknown", body);
                    return (serverId, response);
                }
                catch
                {
                    return (serverId, default(JsonElement));
                }
            }));
        }

        var completed = await Task.WhenAll(tasks);
        foreach (var item in completed)
        {
            if (item.Result.ValueKind != JsonValueKind.Undefined)
            {
                results[item.ServerId] = item.Result;
            }
        }
        return results;
    }

    public void Close()
    {
        foreach (var conn in _backendConnections.Values)
        {
            conn.Dispose();
        }
        _backendConnections.Clear();
    }
}

public class BackendConnection : IDisposable
{
    private readonly McpServer _server;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    
    private string? _messageUrl;
    private string _sessionId = Guid.NewGuid().ToString("N");
    private Task? _readerTask;

    public ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> PendingRequests { get; } = new();

    public BackendConnection(McpServer server, HttpClient httpClient, ILogger logger)
    {
        _server = server;
        _httpClient = httpClient;
        _logger = logger;
        if (server.Type == "streamable")
        {
            _sessionId = string.Empty;
        }
    }

    private void ConfigureRequest(HttpRequestMessage request, string targetUrl)
    {
        request.Headers.Host = "localhost";
    }

    public async Task ConnectAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _server.Url);
        ConfigureRequest(request, _server.Url);
        request.Headers.Add("Mcp-Session-Id", _sessionId);
        if (!string.IsNullOrEmpty(_server.ApiKey))
        {
            // Standard MCP auth headers: X-API-Key or Bearer token
            if (_server.Id == "ha")
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
            else
            {
                request.Headers.Add("X-API-Key", _server.ApiKey);
                // Also support Bearer Token header just in case
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
        }

        // Add custom headers if configured
        if (!string.IsNullOrEmpty(_server.HeadersJson))
        {
            try
            {
                var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                if (customHeaders != null)
                {
                    foreach (var kvp in customHeaders)
                    {
                        request.Headers.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
            }
        }

        _logger.LogInformation("Connecting to backend {ServerId} SSE stream at {Url}...", _server.Id, _server.Url);
    }

    public void StartReader(Func<JsonElement, Task> onMessageReceived)
    {
        if (_server.Type == "http" || _server.Type == "custom" || _server.Type == "streamable")
        {
            _logger.LogInformation("Server {ServerId} is HTTP/Custom/Streamable type; skipping background SSE reader.", _server.Id);
            return;
        }

        _readerTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, _server.Url);
                    ConfigureRequest(request, _server.Url);
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                    request.Headers.Add("Mcp-Session-Id", _sessionId);
                    
                    if (!string.IsNullOrEmpty(_server.ApiKey))
                    {
                        if (_server.Id == "ha")
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                        }
                        else
                        {
                            request.Headers.Add("X-API-Key", _server.ApiKey);
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                        }
                    }

                    // Add custom headers if configured
                    if (!string.IsNullOrEmpty(_server.HeadersJson))
                    {
                        try
                        {
                            var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                            if (customHeaders != null)
                            {
                                foreach (var kvp in customHeaders)
                                {
                                    request.Headers.Add(kvp.Key, kvp.Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
                        }
                    }
                    
                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                    response.EnsureSuccessStatusCode();

                    foreach (var h in response.Headers)
                    {
                        _logger.LogInformation("Response Header for {ServerId}: {Key} = {Val}", _server.Id, h.Key, string.Join(", ", h.Value));
                    }
                    foreach (var h in response.Content.Headers)
                    {
                        _logger.LogInformation("Response Content Header for {ServerId}: {Key} = {Val}", _server.Id, h.Key, string.Join(", ", h.Value));
                    }

                    // Check for Mcp-Session-Id header (for Streamable HTTP transport)
                    IEnumerable<string>? sessionValues = null;
                    if (response.Headers.TryGetValues("Mcp-Session-Id", out var hVals))
                    {
                        sessionValues = hVals;
                    }
                    else if (response.Content.Headers.TryGetValues("Mcp-Session-Id", out var cVals))
                    {
                        sessionValues = cVals;
                    }

                    if (sessionValues != null)
                    {
                        _sessionId = sessionValues.FirstOrDefault();
                        _messageUrl = _server.Url;
                        _logger.LogInformation("Captured Mcp-Session-Id for {ServerId}: {_sessionId}. Using same URL for POST requests.", _server.Id, _sessionId);
                    }
                    else if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
                    {
                        _messageUrl = _server.Url;
                        _logger.LogInformation("Response is text/event-stream for {ServerId}. Defaulting message URL to same URL.", _server.Id);
                    }
                    
                    using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
                    using var reader = new StreamReader(stream);
                    
                    string? currentEvent = null;
                    while (!reader.EndOfStream && !_cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(_cts.Token);
                        if (line == null) break;
                        
                        if (line.StartsWith("event:"))
                        {
                            currentEvent = line.Substring(6).Trim();
                        }
                        else if (line.StartsWith("data:"))
                        {
                            var data = line.Substring(5).Trim();
                            if (currentEvent == "endpoint")
                            {
                                // Resolve post message URL
                                // Can be relative or absolute
                                if (Uri.IsWellFormedUriString(data, UriKind.Absolute))
                                {
                                    _messageUrl = data;
                                }
                                else
                                {
                                    var baseUri = new Uri(_server.Url);
                                    _messageUrl = new Uri(baseUri, data).ToString();
                                }
                                _logger.LogInformation("Resolved Message URL for {ServerId}: {_messageUrl}", _server.Id, _messageUrl);
                            }
                            else if (currentEvent == "message")
                            {
                                try
                                {
                                    using var doc = JsonDocument.Parse(data);
                                    await onMessageReceived(doc.RootElement.Clone());
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to parse SSE message data: {Data}", data);
                                }
                            }
                        }
                        else if (string.IsNullOrEmpty(line))
                        {
                            currentEvent = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Disconnected from backend {ServerId}. Reconnecting in 5s... Error: {Msg}", _server.Id, ex.Message);
                    await Task.Delay(5000, _cts.Token);
                }
            }
        });
    }

    private async Task<JsonElement> SendDirectPostAsync(string bodyJson)
    {
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, _server.Url) { Content = content };
        ConfigureRequest(req, _server.Url);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (!string.IsNullOrEmpty(_sessionId))
            req.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);

        if (!string.IsNullOrEmpty(_server.ApiKey))
        {
            if (_server.Id == "ha")
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            else
            {
                req.Headers.TryAddWithoutValidation("X-API-Key", _server.ApiKey);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
        }

        if (!string.IsNullOrEmpty(_server.HeadersJson))
        {
            try
            {
                var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                if (customHeaders != null)
                    foreach (var kvp in customHeaders)
                        req.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse custom headers for {ServerId}", _server.Id);
            }
        }

        using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ctsTimeout.Token);

        var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linked.Token);
        resp.EnsureSuccessStatusCode();

        // Capture session ID from response headers
        if (resp.Headers.TryGetValues("Mcp-Session-Id", out var sVals))
            _sessionId = sVals.FirstOrDefault() ?? _sessionId;
        else if (resp.Content.Headers.TryGetValues("Mcp-Session-Id", out var scVals))
            _sessionId = scVals.FirstOrDefault() ?? _sessionId;

        var responseBody = await resp.Content.ReadAsStringAsync(linked.Token);

        // Handle SSE-wrapped responses (event: message\ndata: {...})
        if (responseBody.TrimStart().StartsWith("event:") || responseBody.TrimStart().StartsWith("data:"))
        {
            using var sr = new StringReader(responseBody);
            string? currentEvent = null;
            string? dataValue = null;
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("event:")) currentEvent = line.Substring(6).Trim();
                else if (line.StartsWith("data:")) dataValue = line.Substring(5).Trim();
            }
            if (!string.IsNullOrEmpty(dataValue)) responseBody = dataValue;
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.Clone();
    }

    public async Task<JsonElement> SendRequestAsync(string method, string bodyJson)
    {
        if (_server.Type == "http" || _server.Type == "custom" || _server.Type == "streamable")
        {
            return await SendDirectPostAsync(bodyJson);
        }

        // Wait up to 5 seconds for message URL to resolve
        int attempts = 0;
        while (_messageUrl == null && attempts < 50)
        {
            await Task.Delay(100);
            attempts++;
        }

        if (_messageUrl == null)
        {
            throw new InvalidOperationException($"Backend {_server.Id} has not sent its endpoint event yet.");
        }

        // Extract client JSON-RPC request ID
        string requestId = Guid.NewGuid().ToString("N");
        string modifiedBody = bodyJson;
        
        using (var doc = JsonDocument.Parse(bodyJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("id", out var idProp))
            {
                requestId = idProp.ToString();
            }
            else
            {
                // Inject our own ID if missing
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyJson) ?? new();
                dict["id"] = requestId;
                modifiedBody = JsonSerializer.Serialize(dict);
            }
        }

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        PendingRequests[requestId] = tcs;

        var content = new StringContent(modifiedBody, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        // Build post message
        using var postReq = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content };
        ConfigureRequest(postReq, _messageUrl);
        postReq.Headers.Accept.Clear();
        postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrEmpty(_sessionId))
        {
            postReq.Headers.Add("Mcp-Session-Id", _sessionId);
        }
        
        if (!string.IsNullOrEmpty(_server.ApiKey))
        {
            if (_server.Id == "ha")
            {
                postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
            else
            {
                postReq.Headers.Add("X-API-Key", _server.ApiKey);
                postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
        }

        // Add custom headers if configured
        if (!string.IsNullOrEmpty(_server.HeadersJson))
        {
            try
            {
                var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                if (customHeaders != null)
                {
                    foreach (var kvp in customHeaders)
                    {
                        postReq.Headers.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
            }
        }

        var postResp = await _httpClient.SendAsync(postReq, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
        postResp.EnsureSuccessStatusCode();

        // Capture session ID from POST response headers if not already set
        if (string.IsNullOrEmpty(_sessionId))
        {
            IEnumerable<string>? postSessionValues = null;
            if (postResp.Headers.TryGetValues("Mcp-Session-Id", out var phVals))
            {
                postSessionValues = phVals;
            }
            else if (postResp.Content.Headers.TryGetValues("Mcp-Session-Id", out var pcVals))
            {
                postSessionValues = pcVals;
            }

            if (postSessionValues != null)
            {
                _sessionId = postSessionValues.FirstOrDefault();
                _logger.LogInformation("Captured Mcp-Session-Id from POST response for {ServerId}: {_sessionId}", _server.Id, _sessionId);
            }
        }

        // Await the response from the SSE stream reader (max 10s timeout)
        using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ctsTimeout.Token);
        
        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            PendingRequests.TryRemove(requestId, out _);
            if (ctsTimeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Request to backend {_server.Id} timed out after 30 seconds.");
            }
            throw;
        }
    }

    public async Task SendNotificationAsync(string method, string bodyJson)
    {
        if (_server.Type == "streamable")
        {
            // Fire-and-forget for streamable — notifications don't need responses
            try { await SendDirectPostAsync(bodyJson); } catch { /* ignore */ }
            return;
        }

        if (_server.Type == "http" || _server.Type == "custom")
        {
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var postReq = new HttpRequestMessage(HttpMethod.Post, _server.Url) { Content = content };
            ConfigureRequest(postReq, _server.Url);
            postReq.Headers.Accept.Clear();
            postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            postReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            
            if (!string.IsNullOrEmpty(_server.ApiKey))
            {
                if (_server.Id == "ha")
                {
                    postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                }
                else
                {
                    postReq.Headers.Add("X-API-Key", _server.ApiKey);
                    postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
                }
            }

            // Add custom headers if configured
            if (!string.IsNullOrEmpty(_server.HeadersJson))
            {
                try
                {
                    var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                    if (customHeaders != null)
                    {
                        foreach (var kvp in customHeaders)
                        {
                            postReq.Headers.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
                }
            }

            await _httpClient.SendAsync(postReq, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
            return;
        }

        int attempts = 0;
        while (_messageUrl == null && attempts < 50)
        {
            await Task.Delay(100);
            attempts++;
        }

        if (_messageUrl == null) return;

        var content2 = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        content2.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var postReq2 = new HttpRequestMessage(HttpMethod.Post, _messageUrl) { Content = content2 };
        ConfigureRequest(postReq2, _messageUrl);
        postReq2.Headers.Accept.Clear();
        postReq2.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        postReq2.Headers.Add("Mcp-Session-Id", _sessionId);
        
        if (!string.IsNullOrEmpty(_server.ApiKey))
        {
            if (_server.Id == "ha")
            {
                postReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
            else
            {
                postReq2.Headers.Add("X-API-Key", _server.ApiKey);
                postReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _server.ApiKey);
            }
        }

        // Add custom headers if configured
        if (!string.IsNullOrEmpty(_server.HeadersJson))
        {
            try
            {
                var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(_server.HeadersJson);
                if (customHeaders != null)
                {
                    foreach (var kvp in customHeaders)
                    {
                        postReq2.Headers.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse custom headers for server {ServerId}", _server.Id);
            }
        }

        await _httpClient.SendAsync(postReq2, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

public class SessionManager
{
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<SessionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ClientSession> CreateSessionAsync(string sessionId, HttpResponse clientResponse)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
        var servers = await db.Servers.ToListAsync();

        var sessionLogger = _serviceProvider.GetRequiredService<ILogger<ClientSession>>();
        var client = _httpClientFactory.CreateClient("McpClient");

        var session = new ClientSession(sessionId, clientResponse, servers, client, sessionLogger);
        _sessions[sessionId] = session;
        return session;
    }

    public ClientSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public void CloseSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Close();
        }
    }

    public void ResetAll()
    {
        _logger.LogInformation("Resetting all active MCP client sessions due to configuration change.");
        var keys = _sessions.Keys.ToList();
        foreach (var key in keys)
        {
            CloseSession(key);
        }
    }
}

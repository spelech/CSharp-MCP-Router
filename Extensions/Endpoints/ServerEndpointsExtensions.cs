using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

namespace McpRouter.Extensions
{
    public static class ServerEndpointsExtensions
    {
        public static void MapServerEndpoints(this WebApplication app)
        {
            var api = app.MapGroup("").RequireAuthorization("AdminPolicy");

            api.MapGet("/api/servers", async ([FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                try
                {
                    using var conn = dbFactory.CreateConnection();
                    var rawServers = (await conn.QueryAsync(@"SELECT Id, DisplayName, Url, Enabled, Hidden, Type, Categories, SecretProvider, SecretItemKey, AuthShape, CustomHeaderName, ApiKey, HeadersJson FROM Servers")).ToList();
                    var statuses = sessionManager.BackendStatuses;
                    
                    var sanitized = rawServers.Select(s => {
                        var idStr = Convert.ToString(s.Id) ?? string.Empty;
                        bool isEnabled = false;
                        if (s.Enabled is long longEnabled) isEnabled = longEnabled != 0L;
                        else if (s.Enabled is bool boolEnabled) isEnabled = boolEnabled;
                        else if (s.Enabled != null) isEnabled = Convert.ToBoolean(s.Enabled);

                        bool isHidden = false;
                        if (s.Hidden is long longHidden) isHidden = longHidden != 0L;
                        else if (s.Hidden is bool boolHidden) isHidden = boolHidden;
                        else if (s.Hidden != null) isHidden = Convert.ToBoolean(s.Hidden);
                        var catStr = (string?)s.Categories ?? "[]";
                        List<string> categories;
                        try { categories = JsonSerializer.Deserialize<List<string>>(catStr) ?? new(); }
                        catch { categories = new(); }

                        BackendStatus? status = null;
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            statuses.TryGetValue(idStr, out status);
                        }
                        return new {
                            Id = idStr,
                            DisplayName = (string)s.DisplayName,
                            Url = (string)s.Url,
                            Enabled = isEnabled,
                            Hidden = isHidden,
                            Type = (string)(s.Type ?? "sse"),
                            Categories = categories,
                            SecretProvider = (string)(s.SecretProvider ?? "None"),
                            SecretItemKey = (string?)s.SecretItemKey,
                            AuthShape = (string)(s.AuthShape ?? "bearer"),
                            CustomHeaderName = (string?)s.CustomHeaderName,
                            HeadersJson = (string?)s.HeadersJson,
                            HasApiKey = !string.IsNullOrEmpty((string?)s.ApiKey),
                            ConnectionStatus = isEnabled ? (status?.Status ?? "Disconnected") : "Disabled",
                            ConnectionAttempts = status?.Attempts ?? 0,
                            ConnectionError = status?.Error ?? string.Empty
                        };
                    });
                    return Results.Ok(sanitized);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing GET /api/servers");
                    return Results.Problem(ex.Message);
                }
            });

            api.MapPost("/api/servers/{id}/reconnect", async (string id, [FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, [FromServices] Services.BackendHealthCheckService healthCheckSvc, ILogger<Program> logger) =>
            {
                using var conn = dbFactory.CreateConnection();
                var server = await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @id", new { id });
                if (server == null)
                {
                    return Results.NotFound();
                }

                logger.LogInformation("Triggering manual reconnect request for backend {ServerId} ({DisplayName})", id, server.DisplayName);
                
                await healthCheckSvc.ProbeServerAsync(server);

                var activeSessions = sessionManager.GetActiveSessions();
                foreach (var session in activeSessions)
                {
                    session.StartInitializationForBackend(id);
                }

                return Results.Ok(new { success = true, message = $"Reconnection triggered for server {server.DisplayName}" });
            });

            api.MapPost("/api/servers/reconnect-all", async ([FromServices] Services.BackendHealthCheckService healthCheckSvc) =>
            {
                await healthCheckSvc.ProbeAllServersAsync();
                return Results.Ok(new { success = true });
            });
            
            api.MapPut("/api/servers/{id}", async (string id, [FromBody] McpServer update, [FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                using var conn = dbFactory.CreateConnection();
                var server = await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @id", new { id });
                if (server == null)
                {
                    _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, JsonSerializer.Serialize(update), false, "Server not found");
                    return Results.NotFound();
                }
            
                server.Enabled = update.Enabled;
                server.Hidden = update.Hidden;
                
                if (!string.IsNullOrEmpty(update.DisplayName)) server.DisplayName = update.DisplayName;
                if (!string.IsNullOrEmpty(update.Url))
                {
                    if (!IsValidServerUrl(update.Url, httpContext.RequestServices.GetRequiredService<IConfiguration>(), out var err))
                    {
                        _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, JsonSerializer.Serialize(update), false, err);
                        return Results.BadRequest(new { error = err });
                    }
                    server.Url = update.Url;
                }
                if (!string.IsNullOrEmpty(update.Type)) server.Type = update.Type;
                if (update.SecretProvider != null) server.SecretProvider = update.SecretProvider;
                if (update.SecretItemKey != null) server.SecretItemKey = update.SecretItemKey;
                if (!string.IsNullOrEmpty(update.AuthShape)) server.AuthShape = update.AuthShape;
                if (update.CustomHeaderName != null) server.CustomHeaderName = update.CustomHeaderName;
                if (update.Categories != null) server.Categories = update.Categories;
                if (!string.IsNullOrWhiteSpace(update.ApiKey)) server.ApiKey = update.ApiKey;
                if (update.HeadersJson != null) server.HeadersJson = update.HeadersJson;
                
                var catJson = JsonSerializer.Serialize(server.Categories ?? new());
                await conn.ExecuteAsync(@"UPDATE Servers SET DisplayName = @DisplayName, Url = @Url, Enabled = @Enabled, Hidden = @Hidden, Type = @Type,
                    SecretProvider = @SecretProvider, SecretItemKey = @SecretItemKey, AuthShape = @AuthShape, CustomHeaderName = @CustomHeaderName,
                    Categories = @Categories, ApiKey = @ApiKey, HeadersJson = @HeadersJson WHERE Id = @Id",
                    new {
                        server.DisplayName, server.Url, Enabled = server.Enabled ? 1 : 0, Hidden = server.Hidden ? 1 : 0, server.Type,
                        server.SecretProvider, server.SecretItemKey, server.AuthShape, server.CustomHeaderName,
                        Categories = catJson, server.ApiKey, server.HeadersJson, server.Id
                    });
                
                sessionManager.RemoveServerCache(id);
                sessionManager.ResetAll();
            
                _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, JsonSerializer.Serialize(update), true);

                return Results.Ok(server);
            });
            
            api.MapPost("/api/servers", async ([FromBody] McpServer server, [FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                if (!IsValidServerUrl(server.Url, httpContext.RequestServices.GetRequiredService<IConfiguration>(), out var err))
                {
                    _ = auditLogger.LogAdminActionAsync(username, "CreateServer", server.Id ?? "unknown", JsonSerializer.Serialize(server), false, err);
                    return Results.BadRequest(new { error = err });
                }

                if (string.IsNullOrEmpty(server.Id))
                {
                    server.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                }
                
                using var conn = dbFactory.CreateConnection();
                var catJson = JsonSerializer.Serialize(server.Categories ?? new());
                await conn.ExecuteAsync(@"INSERT INTO Servers (Id, DisplayName, Url, Enabled, Hidden, Type, SecretProvider, SecretItemKey, AuthShape, CustomHeaderName, Categories, ApiKey, HeadersJson)
                    VALUES (@Id, @DisplayName, @Url, @Enabled, @Hidden, @Type, @SecretProvider, @SecretItemKey, @AuthShape, @CustomHeaderName, @Categories, @ApiKey, @HeadersJson)",
                    new {
                        server.Id, server.DisplayName, server.Url, Enabled = server.Enabled ? 1 : 0, Hidden = server.Hidden ? 1 : 0, Type = server.Type ?? "sse",
                        SecretProvider = server.SecretProvider ?? "None", server.SecretItemKey, AuthShape = server.AuthShape ?? "bearer", server.CustomHeaderName,
                        Categories = catJson, server.ApiKey, server.HeadersJson
                    });
                
                sessionManager.ResetAll();

                _ = auditLogger.LogAdminActionAsync(username, "CreateServer", server.Id, JsonSerializer.Serialize(server), true);

                return Results.Ok(server);
            });
            
            api.MapDelete("/api/servers/{id}", async (string id, [FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                using var conn = dbFactory.CreateConnection();
                var server = await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @id", new { id });
                if (server == null)
                {
                    _ = auditLogger.LogAdminActionAsync(username, "DeleteServer", id, "", false, "Server not found");
                    return Results.NotFound();
                }
                
                await conn.ExecuteAsync("DELETE FROM Servers WHERE Id = @id", new { id });
                
                sessionManager.RemoveServerCache(id);
                sessionManager.ResetAll();

                _ = auditLogger.LogAdminActionAsync(username, "DeleteServer", id, "", true);

                return Results.Ok(new { success = true });
            });

            api.MapGet("/api/servers/{id}/inspect", async (string id, [FromServices] IDbConnectionFactory dbFactory, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                using var conn = dbFactory.CreateConnection();
                var server = await conn.QueryFirstOrDefaultAsync<McpServer>("SELECT * FROM Servers WHERE Id = @id", new { id });
                if (server == null) return Results.NotFound(new { error = "Server not found" });

                var sessionId = "inspect-" + id + "-" + Guid.NewGuid().ToString("N")[..8];
                var tools = new List<object>();
                var prompts = new List<object>();
                var resources = new List<object>();

                try
                {
                    var session = await sessionManager.CreateSessionAsync(sessionId, null!, id, false);

                    try
                    {
                        tools = await session.ListToolsAsync("{}");
                    }
                    catch (Exception exTools)
                    {
                        logger.LogWarning(exTools, "Failed to list tools for server {ServerId} during inspect", id);
                    }

                    try
                    {
                        prompts = await session.ListPromptsAsync("{}");
                    }
                    catch (Exception exPrompts)
                    {
                        logger.LogWarning(exPrompts, "Failed to list prompts for server {ServerId} during inspect", id);
                    }

                    try
                    {
                        resources = await session.ListResourcesAsync("{}");
                    }
                    catch (Exception exRes)
                    {
                        logger.LogWarning(exRes, "Failed to list resources for server {ServerId} during inspect", id);
                    }

                    return Results.Ok(new
                    {
                        server = new { id = server.Id, displayName = server.DisplayName, type = server.Type, url = server.Url, enabled = server.Enabled },
                        tools,
                        prompts,
                        resources
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating session or connecting to server {ServerId} during inspect", id);
                    return Results.Ok(new
                    {
                        server = new { id = server.Id, displayName = server.DisplayName, type = server.Type, url = server.Url, enabled = server.Enabled },
                        tools = new List<object>(),
                        prompts = new List<object>(),
                        resources = new List<object>(),
                        error = ex.Message
                    });
                }
                finally
                {
                    sessionManager.CloseSession(sessionId);
                }
            });
        }

        public static bool IsValidServerUrl(string? url, IConfiguration config, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(url))
            {
                errorMessage = "Server URL cannot be empty.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errorMessage = "Server URL must be a valid HTTP or HTTPS URI.";
                return false;
            }

            var host = uri.Host;
            var allowedIpRanges = config.GetSection("Security:AllowedIpRanges").Get<string[]>() ?? Array.Empty<string>();

            try
            {
                System.Net.IPAddress[] ipAddresses;
                if (System.Net.IPAddress.TryParse(host, out var directIp))
                {
                    ipAddresses = new[] { directIp };
                }
                else
                {
                    ipAddresses = System.Net.Dns.GetHostAddresses(host);
                }

                foreach (var ip in ipAddresses)
                {
                    if (McpRouter.Core.Security.SecurityValidationHelper.IsBlockedIp(ip, allowedIpRanges))
                    {
                        errorMessage = $"Access to IP address '{ip}' for host '{host}' is blocked for security (SSRF protection).";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to resolve host '{host}': {ex.Message}";
                return false;
            }

            return true;
        }
    }
}
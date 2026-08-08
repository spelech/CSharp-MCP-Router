using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using McpRouter.Core.Database;
using McpRouter.Services;
using McpRouter.Models;
using McpRouter.Core.Logging;
using Dapper;
using System.Linq;

namespace McpRouter.Extensions
{
    public static class ServerEndpointsExtensions
    {
        public static void MapServerEndpoints(this WebApplication app)
        {
            var api = app.MapGroup("").RequireAuthorization("AdminPolicy");
// ----------------------------------------------------
            // DASHBOARD MANAGEMENT ENDPOINTS
            // ----------------------------------------------------
            api.MapGet("/api/servers", async ([FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager) =>
            {
                var servers = await db.Servers.ToListAsync();
                var statuses = sessionManager.BackendStatuses;
                
                var sanitized = servers.Select(s => {
                    statuses.TryGetValue(s.Id, out var status);
                    return new {
                        s.Id,
                        s.DisplayName,
                        s.Url,
                        s.Enabled,
                        s.Hidden,
                        s.Type,
                        s.Categories,
                        s.SecretProvider,
                        s.SecretItemKey,
                        s.AuthShape,
                        s.CustomHeaderName,
                        s.HeadersJson,
                        HasApiKey = !string.IsNullOrEmpty(s.ApiKey),
                        ConnectionStatus = s.Enabled ? (status?.Status ?? "Disconnected") : "Disabled",
                        ConnectionAttempts = status?.Attempts ?? 0,
                        ConnectionError = status?.Error ?? string.Empty
                    };
                });
                return Results.Ok(sanitized);
            });

            api.MapPost("/api/servers/{id}/reconnect", async (string id, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager, [FromServices] Services.BackendHealthCheckService healthCheckSvc, ILogger<Program> logger) =>
            {
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
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
            
            api.MapPut("/api/servers/{id}", async (string id, [FromBody] McpServer update, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
                if (server == null)
                {
                    _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, System.Text.Json.JsonSerializer.Serialize(update), false, "Server not found");
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
                    if (!IsValidServerUrl(update.Url, httpContext.RequestServices.GetRequiredService<IConfiguration>(), out var err))
                    {
                        _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, System.Text.Json.JsonSerializer.Serialize(update), false, err);
                        return Results.BadRequest(new { error = err });
                    }
                    server.Url = update.Url;
                }
                if (!string.IsNullOrEmpty(update.Type))
                {
                    server.Type = update.Type;
                }
                if (update.SecretProvider != null)
                {
                    server.SecretProvider = update.SecretProvider;
                }
                if (update.SecretItemKey != null)
                {
                    server.SecretItemKey = update.SecretItemKey;
                }
                if (!string.IsNullOrEmpty(update.AuthShape))
                {
                    server.AuthShape = update.AuthShape;
                }
                if (update.CustomHeaderName != null)
                {
                    server.CustomHeaderName = update.CustomHeaderName;
                }
                if (update.Categories != null)
                {
                    server.Categories = update.Categories;
                }
                if (!string.IsNullOrWhiteSpace(update.ApiKey))
                {
                    server.ApiKey = update.ApiKey;
                }
                if (update.HeadersJson != null)
                {
                    server.HeadersJson = update.HeadersJson;
                }
                
                await db.SaveChangesAsync();
                
                // Clear cache only for this server
                sessionManager.RemoveServerCache(id);
                
                // Reset active sessions so they reconnect to updated backends
                sessionManager.ResetAll();
            
                _ = auditLogger.LogAdminActionAsync(username, "UpdateServer", id, System.Text.Json.JsonSerializer.Serialize(update), true);

                return Results.Ok(server);
            });
            
            api.MapPost("/api/servers", async ([FromBody] McpServer server, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                if (!IsValidServerUrl(server.Url, httpContext.RequestServices.GetRequiredService<IConfiguration>(), out var err))
                {
                    _ = auditLogger.LogAdminActionAsync(username, "CreateServer", server.Id ?? "unknown", System.Text.Json.JsonSerializer.Serialize(server), false, err);
                    return Results.BadRequest(new { error = err });
                }

                if (string.IsNullOrEmpty(server.Id))
                {
                    server.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                }
                
                db.Servers.Add(server);
                await db.SaveChangesAsync();
                
                sessionManager.ResetAll();

                _ = auditLogger.LogAdminActionAsync(username, "CreateServer", server.Id, System.Text.Json.JsonSerializer.Serialize(server), true);

                return Results.Ok(server);
            });
            
            api.MapDelete("/api/servers/{id}", async (string id, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager, HttpContext httpContext, [FromServices] IAuditLogger auditLogger) =>
            {
                var username = httpContext.User.Identity?.Name ?? "anonymous";
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
                if (server == null)
                {
                    _ = auditLogger.LogAdminActionAsync(username, "DeleteServer", id, "", false, "Server not found");
                    return Results.NotFound();
                }
                
                db.Servers.Remove(server);
                await db.SaveChangesAsync();
                
                // Clear cache only for this server
                sessionManager.RemoveServerCache(id);
                
                sessionManager.ResetAll();

                _ = auditLogger.LogAdminActionAsync(username, "DeleteServer", id, "", true);

                return Results.Ok(new { success = true });
            });

            api.MapGet("/api/servers/{id}/inspect", async (string id, [FromServices] RouterDbContext db, [FromServices] SessionManager sessionManager, ILogger<Program> logger) =>
            {
                var server = await db.Servers.FirstOrDefaultAsync(s => s.Id == id);
                if (server == null) return Results.NotFound(new { error = "Server not found" });

                try
                {
                    var sessionId = "inspect-" + id + "-" + Guid.NewGuid().ToString("N")[..8];
                    var session = await sessionManager.CreateSessionAsync(sessionId, null!, id, false);
                    var tools = await session.ListToolsAsync("{}");
                    var prompts = await session.ListPromptsAsync("{}");
                    var resources = await session.ListResourcesAsync("{}");
                    sessionManager.CloseSession(sessionId);

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
                    logger.LogError(ex, "Error inspecting server {ServerId}", id);
                    return Results.Problem($"Failed to inspect server capabilities: {ex.Message}");
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
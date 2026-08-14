using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Infrastructure.Persistence;
using McpRouter.Components.Servers;

namespace McpRouter.Core.Routing
{
    public class PendingApproval
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ToolName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public TaskCompletionSource<bool> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SessionManager> _logger;

        public ConcurrentDictionary<string, PendingApproval> PendingApprovals { get; } = new();

        public DateTime StartTime { get; } = DateTime.UtcNow;
        private long _totalRequests = 0;
        public long TotalRequests => _totalRequests;

        private long _totalInputTokens = 0;
        private long _totalOutputTokens = 0;
        private long _totalDurationMs = 0;

        public long TotalInputTokens => _totalInputTokens;
        public long TotalOutputTokens => _totalOutputTokens;
        public long TotalDurationMs => _totalDurationMs;

        public void AddPerformanceMetrics(long inputTokens, long outputTokens, long durationMs)
        {
            System.Threading.Interlocked.Add(ref _totalInputTokens, inputTokens);
            System.Threading.Interlocked.Add(ref _totalOutputTokens, outputTokens);
            System.Threading.Interlocked.Add(ref _totalDurationMs, durationMs);
        }

        public void IncrementTotalRequests()
        {
            System.Threading.Interlocked.Increment(ref _totalRequests);
        }

        public int ActiveSessionsCount => _sessions.Count;

        public ConcurrentDictionary<string, BackendStatus> BackendStatuses { get; } = new();

        public SessionManager(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<SessionManager> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public void UpdateBackendStatus(string serverId, string status, int attempts, string error)
        {
            var bStatus = BackendStatuses.GetOrAdd(serverId, id => new BackendStatus { ServerId = id });
            bStatus.Status = status;
            bStatus.Attempts = attempts;
            bStatus.Error = error;
        }

        public async Task<ClientSession> CreateSessionAsync(string sessionId, HttpResponse clientResponse, string? targetServerId = null, bool metaMode = false)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            using var conn = dbFactory.CreateConnection();
            var rawServers = await conn.QueryAsync<McpServer>("SELECT * FROM Servers WHERE Enabled = 1");
            var servers = rawServers.ToList();

            if (!string.IsNullOrWhiteSpace(targetServerId))
            {
                servers = servers.Where(s =>
                    string.Equals(s.Id, targetServerId, StringComparison.OrdinalIgnoreCase) ||
                    (s.Categories != null && s.Categories.Any(c => string.Equals(c, targetServerId, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            var sessionLogger = _serviceProvider.GetRequiredService<ILogger<ClientSession>>();
            var client = _httpClientFactory.CreateClient("McpClient");
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var session = new ClientSession(sessionId, clientResponse, servers, client, embeddingService, this, sessionLogger, _serviceProvider);
            session.IsMetaMode = metaMode;

            if (_sessions.TryRemove(sessionId, out var oldSession))
            {
                try
                {
                    oldSession.Close();
                }
                catch (Exception exClose)
                {
                    _logger.LogWarning(exClose, "Error closing overwritten session for ID {SessionId}", sessionId);
                }
            }

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
                // Sweep pending approvals for this session
                var toRemove = PendingApprovals.Values.Where(a => a.SessionId == sessionId).ToList();
                foreach (var approval in toRemove)
                {
                    if (PendingApprovals.TryRemove(approval.Id, out var removed))
                    {
                        removed.Tcs.TrySetCanceled();
                    }
                }
            }
        }

        public System.Collections.Generic.List<ClientSession> GetActiveSessions()
        {
            return _sessions.Values.ToList();
        }

        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<object>> _serverToolsCache = new();
        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<object>> _serverPromptsCache = new();
        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<object>> _serverResourcesCache = new();
        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<object>> _serverResourceTemplatesCache = new();

        public System.Collections.Generic.List<object>? GetServerToolsCache(string serverId)
        {
            _serverToolsCache.TryGetValue(serverId, out var tools);
            return tools;
        }

        public void SetServerToolsCache(string serverId, System.Collections.Generic.List<object> tools)
        {
            _serverToolsCache[serverId] = tools;
        }

        public void RemoveServerToolsCache(string serverId)
        {
            _serverToolsCache.TryRemove(serverId, out _);
        }

        public System.Collections.Generic.List<object>? GetServerPromptsCache(string serverId)
        {
            _serverPromptsCache.TryGetValue(serverId, out var prompts);
            return prompts;
        }

        public void SetServerPromptsCache(string serverId, System.Collections.Generic.List<object> prompts)
        {
            _serverPromptsCache[serverId] = prompts;
        }

        public void RemoveServerPromptsCache(string serverId)
        {
            _serverPromptsCache.TryRemove(serverId, out _);
        }

        public System.Collections.Generic.List<object>? GetServerResourcesCache(string serverId)
        {
            _serverResourcesCache.TryGetValue(serverId, out var resources);
            return resources;
        }

        public void SetServerResourcesCache(string serverId, System.Collections.Generic.List<object> resources)
        {
            _serverResourcesCache[serverId] = resources;
        }

        public void RemoveServerResourcesCache(string serverId)
        {
            _serverResourcesCache.TryRemove(serverId, out _);
        }

        public System.Collections.Generic.List<object>? GetServerResourceTemplatesCache(string serverId)
        {
            _serverResourceTemplatesCache.TryGetValue(serverId, out var templates);
            return templates;
        }

        public void SetServerResourceTemplatesCache(string serverId, System.Collections.Generic.List<object> templates)
        {
            _serverResourceTemplatesCache[serverId] = templates;
        }

        public void RemoveServerResourceTemplatesCache(string serverId)
        {
            _serverResourceTemplatesCache.TryRemove(serverId, out _);
        }

        public void RemoveServerCache(string serverId)
        {
            RemoveServerToolsCache(serverId);
            RemoveServerPromptsCache(serverId);
            RemoveServerResourcesCache(serverId);
            RemoveServerResourceTemplatesCache(serverId);
        }

        public void ClearGlobalCache()
        {
            _serverToolsCache.Clear();
            _serverPromptsCache.Clear();
            _serverResourcesCache.Clear();
            _serverResourceTemplatesCache.Clear();
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
}

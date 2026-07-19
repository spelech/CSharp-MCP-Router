using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter
{
    public class BackendStatus
    {
        public string ServerId { get; set; } = string.Empty;
        public string Status { get; set; } = "Disconnected";
        public int Attempts { get; set; } = 0;
        public string Error { get; set; } = string.Empty;
    }

    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SessionManager> _logger;

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
            var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
            
            var query = db.Servers.Where(s => s.Enabled);
            if (!string.IsNullOrWhiteSpace(targetServerId))
            {
                query = query.Where(s => s.Id == targetServerId || s.Category == targetServerId);
            }
            var servers = await query.ToListAsync();

            var sessionLogger = _serviceProvider.GetRequiredService<ILogger<ClientSession>>();
            var client = _httpClientFactory.CreateClient("McpClient");
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var session = new ClientSession(sessionId, clientResponse, servers, client, embeddingService, this, sessionLogger);
            session.IsMetaMode = metaMode;
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

        public List<ClientSession> GetActiveSessions()
        {
            return _sessions.Values.ToList();
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

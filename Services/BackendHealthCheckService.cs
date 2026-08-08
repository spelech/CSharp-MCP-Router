using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpRouter.Core;
using McpRouter.Core.Database;
using McpRouter.Models;
using Dapper;

namespace McpRouter.Services
{
    /// <summary>
    /// Background hosted service that periodically probes downstream backend MCP servers and updates their health status.
    /// </summary>
    public class BackendHealthCheckService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SessionManager _sessionManager;
        private readonly ILogger<BackendHealthCheckService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendHealthCheckService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The application service provider scope factory.</param>
        /// <param name="httpClientFactory">The HTTP client factory for sending health probes.</param>
        /// <param name="sessionManager">The session manager tracking active backend statuses.</param>
        /// <param name="logger">The logger instance.</param>
        public BackendHealthCheckService(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            SessionManager sessionManager,
            ILogger<BackendHealthCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Backend MCP Server Health Check background service...");
            
            // Immediate probe on startup
            await ProbeAllServersAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProbeAllServersAsync();
            }
        }

        public async Task ProbeAllServersAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                List<McpServer> servers = new();
                try
                {
                    var dbFactory = scope.ServiceProvider.GetService<IDbConnectionFactory>();
                    if (dbFactory != null)
                    {
                        using var conn = dbFactory.CreateConnection();
                        var rawServers = (await conn.QueryAsync(@"SELECT Id, DisplayName, Url, Enabled, Hidden, Type, Categories, SecretProvider, SecretItemKey, AuthShape, CustomHeaderName, ApiKey, HeadersJson FROM Servers")).ToList();

                        servers = rawServers.Select(s => new McpServer
                        {
                            Id = Convert.ToString(s.Id) ?? string.Empty,
                            DisplayName = Convert.ToString(s.DisplayName) ?? string.Empty,
                            Url = Convert.ToString(s.Url) ?? string.Empty,
                            Enabled = s.Enabled is long l ? l != 0L : Convert.ToBoolean(s.Enabled),
                            Hidden = s.Hidden is long lh ? lh != 0L : Convert.ToBoolean(s.Hidden),
                            Type = Convert.ToString(s.Type) ?? "sse",
                            ApiKey = Convert.ToString(s.ApiKey)
                        }).Where(s => s.Enabled).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Dapper query failed, falling back to RouterDbContext");
                }

                if (servers.Count == 0)
                {
                    var db = scope.ServiceProvider.GetService<RouterDbContext>();
                    if (db != null)
                    {
                        servers = await db.Servers.Where(s => s.Enabled).ToListAsync();
                    }
                }

                var tasks = servers.Select(s => ProbeServerAsync(s));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred during backend servers health check sweep");
            }
        }

        public async Task ProbeServerAsync(McpServer server)
        {
            if (!server.Enabled)
            {
                _sessionManager.UpdateBackendStatus(server.Id, "Disabled", 0, "");
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("McpClient");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                var request = new HttpRequestMessage(HttpMethod.Get, server.Url);
                if (!string.IsNullOrEmpty(server.ApiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {server.ApiKey}");
                    request.Headers.Add("X-API-Key", server.ApiKey);
                }

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                // Any response means the server container / network process is listening and healthy
                _sessionManager.UpdateBackendStatus(server.Id, "Connected", 1, "");
            }
            catch (Exception ex)
            {
                _sessionManager.UpdateBackendStatus(server.Id, "Failed", 1, ex.Message);
            }
        }
    }
}

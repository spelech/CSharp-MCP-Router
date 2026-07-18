using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using McpRouter.Models;

namespace McpRouter.Services
{
    public class DynamicEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<DynamicEmbeddingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;

        private IEmbeddingService? _activeService;
        private RouterSettings _settings = new();
        private readonly object _lock = new();

        public DynamicEmbeddingService(HttpClient httpClient, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<DynamicEmbeddingService>();

            LoadSettings();
        }

        public RouterSettings GetSettings()
        {
            lock (_lock)
            {
                return _settings;
            }
        }

        public void SaveSettings(RouterSettings newSettings)
        {
            lock (_lock)
            {
                _settings = newSettings;
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
                    
                    var dbSettings = db.Settings.FirstOrDefault(s => s.Id == "default");
                    if (dbSettings == null)
                    {
                        db.Settings.Add(_settings);
                    }
                    else
                    {
                        dbSettings.EmbeddingProvider = _settings.EmbeddingProvider;
                        dbSettings.EmbeddingApiUrl = _settings.EmbeddingApiUrl;
                        dbSettings.EmbeddingApiKey = _settings.EmbeddingApiKey;
                        dbSettings.EmbeddingApiModel = _settings.EmbeddingApiModel;
                        dbSettings.EmbeddingModelDir = _settings.EmbeddingModelDir;
                    }
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save settings to the encrypted database");
                }
                ReloadActiveService();
            }
        }

        private void LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RouterDbContext>();
                    
                    var dbSettings = db.Settings.FirstOrDefault(s => s.Id == "default");
                    if (dbSettings != null)
                    {
                        _settings = dbSettings;
                    }
                    else
                    {
                        _settings = new RouterSettings();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load settings from DB, falling back to defaults");
                    _settings = new RouterSettings();
                }
                ReloadActiveService();
            }
        }

        private void ReloadActiveService()
        {
            if (_settings.EmbeddingProvider.ToLower() == "api")
            {
                _logger.LogInformation("Activating external API embedding provider pointing to {Url}", _settings.EmbeddingApiUrl);
                _activeService = new ApiEmbeddingService(_httpClient, _settings);
            }
            else
            {
                _logger.LogInformation("Activating local ONNX in-process embedding provider");
                _activeService = new OnnxEmbeddingService(_httpClient, _settings, _loggerFactory.CreateLogger<OnnxEmbeddingService>());
            }
        }

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            lock (_lock)
            {
                if (_activeService == null) LoadSettings();
                return _activeService!.GetEmbeddingAsync(text);
            }
        }

        public double CosineSimilarity(float[] vector1, float[] vector2)
        {
            lock (_lock)
            {
                if (_activeService == null) LoadSettings();
                return _activeService!.CosineSimilarity(vector1, vector2);
            }
        }

        public void ReloadSettings(RouterSettings settings)
        {
            SaveSettings(settings);
        }
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace McpRouter.Core.Routing
{
    public partial class SessionManager
    {
        private readonly ConcurrentDictionary<string, List<object>> _serverToolsCache = new();
        private readonly ConcurrentDictionary<string, List<object>> _serverPromptsCache = new();
        private readonly ConcurrentDictionary<string, List<object>> _serverResourcesCache = new();
        private readonly ConcurrentDictionary<string, List<object>> _serverResourceTemplatesCache = new();

        public List<object>? GetServerToolsCache(string serverId)
        {
            _serverToolsCache.TryGetValue(serverId, out var tools);
            return tools;
        }

        public void SetServerToolsCache(string serverId, List<object> tools)
        {
            _serverToolsCache[serverId] = tools;
        }

        public void RemoveServerToolsCache(string serverId)
        {
            _serverToolsCache.TryRemove(serverId, out _);
        }

        public List<object>? GetServerPromptsCache(string serverId)
        {
            _serverPromptsCache.TryGetValue(serverId, out var prompts);
            return prompts;
        }

        public void SetServerPromptsCache(string serverId, List<object> prompts)
        {
            _serverPromptsCache[serverId] = prompts;
        }

        public void RemoveServerPromptsCache(string serverId)
        {
            _serverPromptsCache.TryRemove(serverId, out _);
        }

        public List<object>? GetServerResourcesCache(string serverId)
        {
            _serverResourcesCache.TryGetValue(serverId, out var resources);
            return resources;
        }

        public void SetServerResourcesCache(string serverId, List<object> resources)
        {
            _serverResourcesCache[serverId] = resources;
        }

        public void RemoveServerResourcesCache(string serverId)
        {
            _serverResourcesCache.TryRemove(serverId, out _);
        }

        public List<object>? GetServerResourceTemplatesCache(string serverId)
        {
            _serverResourceTemplatesCache.TryGetValue(serverId, out var templates);
            return templates;
        }

        public void SetServerResourceTemplatesCache(string serverId, List<object> templates)
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
    }
}

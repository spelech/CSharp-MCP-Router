using System.Collections.Generic;
using McpRouter.Models;
namespace McpRouter.CustomTools
{
public static class CustomToolRegistry
    {
        private static readonly Dictionary<string, ICustomTool> _tools = new();

        static CustomToolRegistry()
        {
            Register(new SeerrSearchMediaTool());
            Register(new SeerrRequestMediaTool());
            Register(new SeerrGetRequestsTool());
            Register(new SeerrGetMediaDetailsTool());
            Register(new PlexSearchLibraryTool());
            Register(new PlexGetLibrarySectionsTool());
            Register(new PlexGetSessionsTool());
            Register(new PlexGetRecentlyAddedTool());
            Register(new PlexGetMetadataTool());
        }

        private static void Register(ICustomTool tool)
        {
            _tools[tool.Name] = tool;
        }

        public static IEnumerable<ICustomTool> GetAll() => _tools.Values;

        public static ICustomTool? Get(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }
}

using System.Text.Json;

namespace McpRouter.Components.Capabilities
{
    public class TestToolCallModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestCallModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestPromptGetModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string PromptName { get; set; } = string.Empty;
        public JsonElement Arguments { get; set; }
    }

    public class TestResourceReadModel
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class SearchModel
    {
        public string Query { get; set; } = string.Empty;
    }
}

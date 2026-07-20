using System.ComponentModel.DataAnnotations;

namespace McpRouter.Models
{
    public class RouterSettings
    {
        [Key]
        public string Id { get; set; } = "default";
        public string EmbeddingProvider { get; set; } = "local"; // "local" or "api"
        public string EmbeddingApiUrl { get; set; } = "http://litellm:4000/v1/embeddings";
        public string EmbeddingApiKey { get; set; } = "";
        public string EmbeddingApiModel { get; set; } = "all-MiniLM-L6-v2";
        public string EmbeddingModelDir { get; set; } = "data/models";
        public bool RequireManualApproval { get; set; } = false;
    }
}

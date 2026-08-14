using System.Threading.Tasks;
using McpRouter.Models;

namespace McpRouter.Core.Routing
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
        double CosineSimilarity(float[] vector1, float[] vector2);
        void ReloadSettings(RouterSettings settings);
    }
}

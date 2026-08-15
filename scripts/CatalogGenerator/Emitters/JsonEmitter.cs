using System.IO;
using System.Linq;
using System.Text.Json;
using CatalogGenerator.Models;

namespace CatalogGenerator.Emitters
{
    public static class JsonEmitter
    {
        public static string Emit(CatalogIndex index)
        {
            var all = index.GetAll().ToList();
            var payload = new
            {
                metadata = new
                {
                    generatedAt = System.DateTime.UtcNow.ToString("o"),
                    totalRequirements = all.Count,
                    positiveCount = all.Count(r => r.Type == RequirementType.Positive),
                    guardrailCount = all.Count(r => r.Type == RequirementType.Negative),
                    totalProofs = all.Sum(r => r.Proofs.Count)
                },
                requirements = all.Select(r => new
                {
                    id = r.Id,
                    category = r.Category,
                    type = r.Type.ToString(),
                    description = r.Description,
                    proofCount = r.Proofs.Count,
                    proofs = r.Proofs.Select(p => new
                    {
                        suite = p.Suite,
                        filePath = p.FilePath,
                        lineNumber = p.LineNumber,
                        testName = p.TestName,
                        details = p.Details
                    })
                })
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

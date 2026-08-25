using CatalogGenerator.Emitters;
using CatalogGenerator.Models;
using Xunit;

namespace CatalogGenerator.Tests
{
    public class EmitterTests
    {
        [Fact]
        public void MarkdownEmitter_EmitsCategoriesAlertBoxesAndTraceabilityTable()
        {
            var index = new CatalogIndex();
            index.AddOrMergeProof("AUTH-01", "AUTH", RequirementType.Positive, "Admin SID bypasses all policies",
                new TestCaseProof("Backend xUnit", "McpRouter.Tests/AdminTests.cs", 34, "AdminSid_Bypasses"));
            index.AddOrMergeProof("GUARD-01", "GUARD", RequirementType.Negative, "Expired keys fail closed",
                new TestCaseProof("Playwright E2E", "frontend/e2e/rbac.spec.ts", 45, "denies expired key"));

            var md = MarkdownEmitter.Emit(index);

            Assert.Contains("# Software Requirements Specification (SRS) & Test Verification Catalog", md);
            Assert.Contains("`AUTH-01`", md);
            Assert.Contains("`GUARD-01`", md);
            Assert.Contains("> [!IMPORTANT]", md);
            Assert.Contains("| `AUTH-01` | Positive |", md);
        }

        [Fact]
        public void JsonEmitter_EmitsValidJsonStructure()
        {
            var index = new CatalogIndex();
            index.AddOrMergeProof("AUTH-01", "AUTH", RequirementType.Positive, "Admin SID bypass",
                new TestCaseProof("Backend xUnit", "McpRouter.Tests/AdminTests.cs", 34, "AdminSid_Bypasses"));

            var json = JsonEmitter.Emit(index);

            Assert.Contains("\"id\": \"AUTH-01\"", json);
            Assert.Contains("\"type\": \"Positive\"", json);
            Assert.Contains("\"totalRequirements\": 1", json);
        }
    }
}

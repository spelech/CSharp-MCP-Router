using System;
using System.Linq;
using Xunit;
using McpRouter.Tests.Attributes;
using CatalogGenerator.Models;

namespace CatalogGenerator.Tests
{
    public class ModelAndAttributeTests
    {
        [Fact]
        [Requirement("TEST-01", "Requirement attribute correctly stores ID, Category, and Type", Type = McpRouter.Tests.Attributes.RequirementType.Positive, Category = "TEST")]
        public void RequirementAttribute_SetsPropertiesCorrectly()
        {
            var attr = new RequirementAttribute("AUTH-01", "Admin SID bypass")
            {
                Type = McpRouter.Tests.Attributes.RequirementType.Positive,
                Category = "AUTH"
            };

            Assert.Equal("AUTH-01", attr.Id);
            Assert.Equal("Admin SID bypass", attr.Description);
            Assert.Equal(McpRouter.Tests.Attributes.RequirementType.Positive, attr.Type);
            Assert.Equal("AUTH", attr.Category);
        }

        [Fact]
        public void RequirementItem_MergesTestProofsCorrectly()
        {
            var item = new RequirementItem("AUTH-01", "AUTH", CatalogGenerator.Models.RequirementType.Positive, "Admin SID bypass");
            item.AddProof(new TestCaseProof("Backend Integration", "McpRouter.Tests/AdminTests.cs", 42, "AdminSid_Bypasses"));
            item.AddProof(new TestCaseProof("Playwright E2E", "frontend/e2e/multi-user.spec.ts", 18, "adminUser can view settings"));

            Assert.Equal(2, item.Proofs.Count);
            Assert.Equal("AUTH-01", item.Id);
        }
    }
}

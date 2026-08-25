using CatalogGenerator.Models;
using ModelContextGateway.Tests.Attributes;
using Xunit;

namespace CatalogGenerator.Tests
{
    public class ModelAndAttributeTests
    {
        [Fact]
        [Requirement("TEST-01", "Requirement attribute correctly stores ID, Category, and Type", Type = ModelContextGateway.Tests.Attributes.RequirementType.Positive, Category = "TEST")]
        public void RequirementAttribute_SetsPropertiesCorrectly()
        {
            var attr = new RequirementAttribute("AUTH-01", "Admin SID bypass")
            {
                Type = ModelContextGateway.Tests.Attributes.RequirementType.Positive,
                Category = "AUTH"
            };

            Assert.Equal("AUTH-01", attr.Id);
            Assert.Equal("Admin SID bypass", attr.Description);
            Assert.Equal(ModelContextGateway.Tests.Attributes.RequirementType.Positive, attr.Type);
            Assert.Equal("AUTH", attr.Category);

            // Test 4-argument constructor overload
            var attr4 = new RequirementAttribute("GUARD-02", "GUARD", ModelContextGateway.Tests.Attributes.RequirementType.Negative, "Guardrail description");
            Assert.Equal("GUARD-02", attr4.Id);
            Assert.Equal("GUARD", attr4.Category);
            Assert.Equal(ModelContextGateway.Tests.Attributes.RequirementType.Negative, attr4.Type);
            Assert.Equal("Guardrail description", attr4.Description);

            // Test 3-argument constructor overload (Id, Type, Description) with inferred category
            var attr3 = new RequirementAttribute("TRANS-05", ModelContextGateway.Tests.Attributes.RequirementType.Positive, "Transport description");
            Assert.Equal("TRANS-05", attr3.Id);
            Assert.Equal("TRANS", attr3.Category);
            Assert.Equal(ModelContextGateway.Tests.Attributes.RequirementType.Positive, attr3.Type);
            Assert.Equal("Transport description", attr3.Description);
        }

        [Fact]
        public void RequirementItem_MergesTestProofsCorrectly()
        {
            var item = new RequirementItem("AUTH-01", "AUTH", CatalogGenerator.Models.RequirementType.Positive, "Admin SID bypass");
            item.AddProof(new TestCaseProof("Backend Integration", "ModelContextGateway.Tests/AdminTests.cs", 42, "AdminSid_Bypasses"));
            item.AddProof(new TestCaseProof("Playwright E2E", "frontend/e2e/multi-user.spec.ts", 18, "adminUser can view settings"));

            Assert.Equal(2, item.Proofs.Count);
            Assert.Equal("AUTH-01", item.Id);
        }
    }
}

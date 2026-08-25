using CatalogGenerator.Models;
using CatalogGenerator.Parsers;
using Xunit;

namespace CatalogGenerator.Tests
{
    public class RoslynCSharpParserTests
    {
        [Fact]
        public void ParseSource_ExtractsRequirementAttributeAndXmlSummary()
        {
            var source = @"
using Xunit;
using ModelContextGateway.Tests.Attributes;

namespace ModelContextGateway.Tests
{
    public class SampleTests
    {
        /// <summary>
        /// Ensures admin SID overrides deny policies.
        /// </summary>
        [Fact]
        [Requirement(""AUTH-01"", ""Admin SID bypasses explicit deny policies"", Type = RequirementType.Positive, Category = ""AUTH"")]
        public void AdminSid_Bypasses()
        {
            Assert.True(true);
        }

        /// <summary>
        /// Ensures expired keys fail closed immediately.
        /// </summary>
        [Fact]
        [Requirement(""GUARD-01"", ""Expired AppKeys must fail closed"", Type = RequirementType.Negative, Category = ""GUARD"")]
        public void ExpiredKey_FailsClosed()
        {
            Assert.True(true);
        }
    }
}";

            var index = new CatalogIndex();
            var parser = new RoslynCSharpParser();
            parser.ParseSource("ModelContextGateway.Tests/SampleTests.cs", source, index);

            Assert.Equal(2, index.Requirements.Count);

            var auth01 = index.Requirements["AUTH-01"];
            Assert.Equal("AUTH", auth01.Category);
            Assert.Equal(RequirementType.Positive, auth01.Type);
            Assert.Equal("Admin SID bypasses explicit deny policies", auth01.Description);
            Assert.Single(auth01.Proofs);
            Assert.Equal("AdminSid_Bypasses", auth01.Proofs[0].TestName);
            Assert.Equal("ModelContextGateway.Tests/SampleTests.cs", auth01.Proofs[0].FilePath);
            Assert.Equal("Ensures admin SID overrides deny policies.", auth01.Proofs[0].Details);

            var guard01 = index.Requirements["GUARD-01"];
            Assert.Equal("GUARD", guard01.Category);
            Assert.Equal(RequirementType.Negative, guard01.Type);
            Assert.Equal("Expired AppKeys must fail closed", guard01.Description);
            Assert.Single(guard01.Proofs);
            Assert.Equal("ExpiredKey_FailsClosed", guard01.Proofs[0].TestName);
        }

        [Fact]
        public void ParseSource_FallbackCategoryAndDescription_WhenNotSpecified()
        {
            var source = @"
using Xunit;
using ModelContextGateway.Tests.Attributes;

namespace ModelContextGateway.Tests
{
    public class FallbackTests
    {
        /// <summary>
        /// Fallback XML summary description.
        /// </summary>
        [Fact]
        [Requirement(""ROUTER-05"")]
        public void Test_Without_Explicit_Desc()
        {
            Assert.True(true);
        }
    }
}";

            var index = new CatalogIndex();
            var parser = new RoslynCSharpParser();
            parser.ParseSource("ModelContextGateway.Tests/FallbackTests.cs", source, index);

            Assert.Single(index.Requirements);
            var req = index.Requirements["ROUTER-05"];
            Assert.Equal("ROUTER", req.Category);
            Assert.Equal("Fallback XML summary description.", req.Description);
            Assert.Equal(RequirementType.Positive, req.Type);
        }

        [Fact]
        public void ParseDirectory_ParsesAllMatchingFilesAndSkipsBinObj()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "RoslynParserTest_" + Path.GetRandomFileName());
            var binDir = Path.Combine(tempDir, "bin", "Debug");
            Directory.CreateDirectory(binDir);

            try
            {
                var validFile = Path.Combine(tempDir, "Test1.cs");
                File.WriteAllText(validFile, @"
using Xunit;
namespace ModelContextGateway.Tests {
    public class T1 {
        [Fact]
        [Requirement(""DIR-01"", ""Dir test"")]
        public void M1() {}
    }
}");
                var binFile = Path.Combine(binDir, "Ignored.cs");
                File.WriteAllText(binFile, @"
using Xunit;
namespace ModelContextGateway.Tests {
    public class Ignored {
        [Fact]
        [Requirement(""BIN-01"", ""Should be ignored"")]
        public void M2() {}
    }
}");

                var index = new CatalogIndex();
                var parser = new RoslynCSharpParser();
                parser.ParseDirectory(tempDir, index);

                Assert.Single(index.Requirements);
                Assert.True(index.Requirements.ContainsKey("DIR-01"));
                Assert.False(index.Requirements.ContainsKey("BIN-01"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        [Fact]
        public void ParseSource_ParsesAllConstructorOverloadPermutations()
        {
            var source = @"
using Xunit;
using ModelContextGateway.Tests.Attributes;

namespace ModelContextGateway.Tests
{
    public class OverloadTests
    {
        [Fact]
        [Requirement(""AUTH-01"", ""AUTH"", RequirementType.Positive, ""Positional 4 args"")]
        public void Test_Positional4() {}

        [Fact]
        [Requirement(""GUARD-02"", RequirementType.Negative, ""Positional 3 args with type"")]
        public void Test_Positional3Type() {}

        [Fact]
        [Requirement(""TRANS-03"", ""Positional 2 args default positive"")]
        public void Test_Positional2() {}

        [Fact]
        [Requirement(""SEC-04"", ""Positional 2 with named overrides"", Type = RequirementType.Negative, Category = ""SECURITY"")]
        public void Test_NamedOverrides() {}
    }
}";

            var index = new CatalogIndex();
            var parser = new RoslynCSharpParser();
            parser.ParseSource("ModelContextGateway.Tests/OverloadTests.cs", source, index);

            Assert.Equal(4, index.Requirements.Count);

            var r1 = index.Requirements["AUTH-01"];
            Assert.Equal("AUTH", r1.Category);
            Assert.Equal(RequirementType.Positive, r1.Type);
            Assert.Equal("Positional 4 args", r1.Description);

            var r2 = index.Requirements["GUARD-02"];
            Assert.Equal("GUARD", r2.Category);
            Assert.Equal(RequirementType.Negative, r2.Type);
            Assert.Equal("Positional 3 args with type", r2.Description);

            var r3 = index.Requirements["TRANS-03"];
            Assert.Equal("TRANS", r3.Category);
            Assert.Equal(RequirementType.Positive, r3.Type);
            Assert.Equal("Positional 2 args default positive", r3.Description);

            var r4 = index.Requirements["SEC-04"];
            Assert.Equal("SECURITY", r4.Category);
            Assert.Equal(RequirementType.Negative, r4.Type);
            Assert.Equal("Positional 2 with named overrides", r4.Description);
        }
    }
}

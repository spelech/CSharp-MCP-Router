# Automated Software Requirements & Test Verification Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an automated CLI tool and annotation framework that extracts requirement metadata from C# xUnit, Vitest, and Playwright tests to generate a comprehensive Software Requirements Specification (SRS) & Test Catalog documenting what the app does (positive features) and does not do (guardrails/fail-closed invariants).

**Architecture:** A .NET 10 Roslyn C# syntax parser and TypeScript AST/JSDoc parser in `/scripts/CatalogGenerator` aggregate test proof metadata across backend and frontend suites. The engine emits `docs/software-requirements-and-test-catalog.md` and `docs/requirements-catalog.json` with full requirement-to-test traceability.

**Tech Stack:** .NET 10, Roslyn (`Microsoft.CodeAnalysis.CSharp`), TypeScript AST / JSDoc regex parsing, xUnit, Vitest, Playwright, React 19, Markdown, JSON Schema.

## Global Constraints
- Target Version: `v4.15.0` (Mandatory version bump across `mcp-router.csproj`, `frontend/package.json`, `useUserStore.ts`, `CHANGELOG.md`, and `README.md`).
- ID Format: `{CATEGORY}-{NUMBER}` (e.g. `AUTH-01`, `MCP-01`, `TRANS-01`, `SEC-01`, `DB-01`, `UI-01`, `GUARD-01`).
- Requirement Types: `Positive` ("What the App Does") vs `Negative` ("What the App Does NOT Do" / Guardrails & Fail-Closed).
- Agent rules in `AGENTS.md` and `.agents/GEMINI.md` must be updated to mandate requirement annotations for all new tests.

---

### Task 1: Scaffolding, Models & C# RequirementAttribute

**Files:**
- Create: `scripts/CatalogGenerator/CatalogGenerator.csproj`
- Create: `scripts/CatalogGenerator/Models/RequirementType.cs`
- Create: `scripts/CatalogGenerator/Models/TestCaseProof.cs`
- Create: `scripts/CatalogGenerator/Models/RequirementItem.cs`
- Create: `scripts/CatalogGenerator/Models/CatalogIndex.cs`
- Create: `McpRouter.Tests/Attributes/RequirementAttribute.cs`
- Test: `scripts/CatalogGenerator/Tests/ModelAndAttributeTests.cs`

**Interfaces:**
- Produces: `RequirementAttribute` (in `McpRouter.Tests`) implementing `Xunit.Sdk.ITraitAttribute`.
- Produces: `RequirementItem`, `TestCaseProof`, `CatalogIndex` data models for the parser pipeline.

- [ ] **Step 1: Write the failing test for requirement models and attribute reflection**

```csharp
// scripts/CatalogGenerator/Tests/ModelAndAttributeTests.cs
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
        [Requirement("TEST-01", "Requirement attribute correctly stores ID, Category, and Type", Type = RequirementType.Positive, Category = "TEST")]
        public void RequirementAttribute_SetsPropertiesCorrectly()
        {
            var attr = new RequirementAttribute("AUTH-01", "Admin SID bypass")
            {
                Type = RequirementType.Positive,
                Category = "AUTH"
            };

            Assert.Equal("AUTH-01", attr.Id);
            Assert.Equal("Admin SID bypass", attr.Description);
            Assert.Equal(RequirementType.Positive, attr.Type);
            Assert.Equal("AUTH", attr.Category);
        }

        [Fact]
        public void RequirementItem_MergesTestProofsCorrectly()
        {
            var item = new RequirementItem("AUTH-01", "AUTH", RequirementType.Positive, "Admin SID bypass");
            item.AddProof(new TestCaseProof("Backend Integration", "McpRouter.Tests/AdminTests.cs", 42, "AdminSid_Bypasses"));
            item.AddProof(new TestCaseProof("Playwright E2E", "frontend/e2e/multi-user.spec.ts", 18, "adminUser can view settings"));

            Assert.Equal(2, item.Proofs.Count);
            Assert.Equal("AUTH-01", item.Id);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj`
Expected: FAIL (project and types not defined)

- [ ] **Step 3: Create CatalogGenerator project file and domain models**

```xml
<!-- scripts/CatalogGenerator/CatalogGenerator.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>CatalogGenerator</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
</Project>
```

```csharp
// McpRouter.Tests/Attributes/RequirementAttribute.cs
using System;
using Xunit.Sdk;

namespace McpRouter.Tests.Attributes
{
    public enum RequirementType
    {
        Positive,
        Negative
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirementAttribute : Attribute, ITraitAttribute
    {
        public string Id { get; }
        public string Description { get; }
        public RequirementType Type { get; set; } = RequirementType.Positive;
        public string? Category { get; set; }

        public RequirementAttribute(string id, string description)
        {
            Id = id;
            Description = description;
            if (string.IsNullOrEmpty(Category) && id.Contains('-'))
            {
                Category = id.Substring(0, id.IndexOf('-'));
            }
        }
    }
}
```

```csharp
// scripts/CatalogGenerator/Models/RequirementType.cs
namespace CatalogGenerator.Models
{
    public enum RequirementType
    {
        Positive,
        Negative
    }
}
```

```csharp
// scripts/CatalogGenerator/Models/TestCaseProof.cs
namespace CatalogGenerator.Models
{
    public record TestCaseProof(
        string Suite,
        string FilePath,
        int LineNumber,
        string TestName,
        string? Details = null
    );
}
```

```csharp
// scripts/CatalogGenerator/Models/RequirementItem.cs
using System.Collections.Generic;

namespace CatalogGenerator.Models
{
    public class RequirementItem
    {
        public string Id { get; }
        public string Category { get; set; }
        public RequirementType Type { get; set; }
        public string Description { get; set; }
        public List<TestCaseProof> Proofs { get; } = new();

        public RequirementItem(string id, string category, RequirementType type, string description)
        {
            Id = id;
            Category = category;
            Type = type;
            Description = description;
        }

        public void AddProof(TestCaseProof proof)
        {
            Proofs.Add(proof);
        }
    }
}
```

```csharp
// scripts/CatalogGenerator/Models/CatalogIndex.cs
using System.Collections.Generic;
using System.Linq;

namespace CatalogGenerator.Models
{
    public class CatalogIndex
    {
        public Dictionary<string, RequirementItem> Requirements { get; } = new();

        public void AddOrMergeProof(string id, string category, RequirementType type, string description, TestCaseProof proof)
        {
            if (!Requirements.TryGetValue(id, out var item))
            {
                item = new RequirementItem(id, category, type, description);
                Requirements[id] = item;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.Description) && !string.IsNullOrWhiteSpace(description))
                {
                    item.Description = description;
                }
                if (item.Type != type && type == RequirementType.Negative)
                {
                    item.Type = type;
                }
            }

            item.AddProof(proof);
        }

        public IEnumerable<RequirementItem> GetAll() => Requirements.Values.OrderBy(r => r.Category).ThenBy(r => r.Id);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add scripts/CatalogGenerator McpRouter.Tests/Attributes
git commit -m "feat(catalog): add requirement attribute and core index models"
```

---

### Task 2: Roslyn C# AST Parser (`RoslynCSharpParser.cs`)

**Files:**
- Create: `scripts/CatalogGenerator/Parsers/RoslynCSharpParser.cs`
- Test: `scripts/CatalogGenerator/Tests/RoslynCSharpParserTests.cs`

**Interfaces:**
- Consumes: `RequirementItem`, `TestCaseProof`, `CatalogIndex`
- Produces: `RoslynCSharpParser.ParseFile(string filePath, string sourceCode, CatalogIndex index)`

- [ ] **Step 1: Write the failing test for RoslynCSharpParser**

```csharp
// scripts/CatalogGenerator/Tests/RoslynCSharpParserTests.cs
using System.Linq;
using Xunit;
using CatalogGenerator.Models;
using CatalogGenerator.Parsers;

namespace CatalogGenerator.Tests
{
    public class RoslynCSharpParserTests
    {
        [Fact]
        public void ParseSource_ExtractsRequirementAttributeAndXmlSummary()
        {
            var source = @"
using Xunit;
using McpRouter.Tests.Attributes;

namespace McpRouter.Tests
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
            parser.ParseSource("McpRouter.Tests/SampleTests.cs", source, index);

            Assert.Equal(2, index.Requirements.Count);

            var auth01 = index.Requirements["AUTH-01"];
            Assert.Equal("AUTH", auth01.Category);
            Assert.Equal(RequirementType.Positive, auth01.Type);
            Assert.Equal("Admin SID bypasses explicit deny policies", auth01.Description);
            Assert.Single(auth01.Proofs);
            Assert.Equal("AdminSid_Bypasses", auth01.Proofs[0].TestName);

            var guard01 = index.Requirements["GUARD-01"];
            Assert.Equal("GUARD", guard01.Category);
            Assert.Equal(RequirementType.Negative, guard01.Type);
            Assert.Equal("Expired AppKeys must fail closed", guard01.Description);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj --filter "FullyQualifiedName~RoslynCSharpParserTests"`
Expected: FAIL (`RoslynCSharpParser` not found)

- [ ] **Step 3: Implement `RoslynCSharpParser`**

```csharp
// scripts/CatalogGenerator/Parsers/RoslynCSharpParser.cs
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CatalogGenerator.Models;

namespace CatalogGenerator.Parsers
{
    public class RoslynCSharpParser
    {
        public void ParseDirectory(string directoryPath, CatalogIndex index)
        {
            if (!Directory.Exists(directoryPath)) return;

            var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Skip bin, obj, AssemblyInfo
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.EndsWith("GlobalUsings.cs"))
                {
                    continue;
                }

                var code = File.ReadAllText(file);
                ParseSource(file, code, index);
            }
        }

        public void ParseSource(string filePath, string sourceCode, CatalogIndex index)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var reqAttributes = method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Where(a => a.Name.ToString().Contains("Requirement"))
                    .ToList();

                if (!reqAttributes.Any()) continue;

                var lineSpan = tree.GetLineSpan(method.Span);
                var lineNumber = lineSpan.StartLinePosition.Line + 1;
                var methodName = method.Identifier.Text;
                var xmlSummary = ExtractXmlSummary(method);

                foreach (var attr in reqAttributes)
                {
                    if (attr.ArgumentList == null || attr.ArgumentList.Arguments.Count == 0) continue;

                    var idArg = attr.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
                    var descArg = attr.ArgumentList.Arguments.Count > 1 
                        ? attr.ArgumentList.Arguments[1].Expression.ToString().Trim('"') 
                        : (xmlSummary ?? methodName);

                    var type = RequirementType.Positive;
                    var category = idArg.Contains('-') ? idArg.Substring(0, idArg.IndexOf('-')) : "GENERAL";

                    foreach (var namedArg in attr.ArgumentList.Arguments.Where(a => a.NameEquals != null))
                    {
                        var name = namedArg.NameEquals!.Name.Identifier.Text;
                        var val = namedArg.Expression.ToString();

                        if (name == "Type")
                        {
                            if (val.Contains("Negative") || val.Contains("Guardrail"))
                                type = RequirementType.Negative;
                            else
                                type = RequirementType.Positive;
                        }
                        else if (name == "Category")
                        {
                            category = val.Trim('"');
                        }
                    }

                    var proof = new TestCaseProof(
                        Suite: "Backend xUnit",
                        FilePath: filePath.Replace('\\', '/'),
                        LineNumber: lineNumber,
                        TestName: methodName,
                        Details: xmlSummary
                    );

                    index.AddOrMergeProof(idArg, category, type, descArg, proof);
                }
            }
        }

        private string? ExtractXmlSummary(MethodDeclarationSyntax method)
        {
            var trivia = method.GetLeadingTrivia()
                .Select(t => t.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (trivia == null) return null;

            var xmlString = trivia.ToString();
            try
            {
                var cleaned = string.Join("\n", xmlString.Split('\n')
                    .Select(l => l.Trim().TrimStart('/', ' ')));
                var elem = XElement.Parse("<doc>" + cleaned + "</doc>");
                var summary = elem.Element("summary")?.Value.Trim();
                return summary;
            }
            catch
            {
                return null;
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj --filter "FullyQualifiedName~RoslynCSharpParserTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add scripts/CatalogGenerator/Parsers scripts/CatalogGenerator/Tests
git commit -m "feat(catalog): implement Roslyn C# syntax parser for requirement attributes"
```

---

### Task 3: TypeScript AST & JSDoc Parser (`TypeScriptTestParser.cs`)

**Files:**
- Create: `scripts/CatalogGenerator/Parsers/TypeScriptTestParser.cs`
- Test: `scripts/CatalogGenerator/Tests/TypeScriptTestParserTests.cs`

**Interfaces:**
- Consumes: `CatalogIndex`, `TestCaseProof`
- Produces: `TypeScriptTestParser.ParseDirectory(string directoryPath, CatalogIndex index)`

- [ ] **Step 1: Write the failing test for TypeScriptTestParser**

```csharp
// scripts/CatalogGenerator/Tests/TypeScriptTestParserTests.cs
using Xunit;
using CatalogGenerator.Models;
using CatalogGenerator.Parsers;

namespace CatalogGenerator.Tests
{
    public class TypeScriptTestParserTests
    {
        [Fact]
        public void ParseTypeScript_ExtractsJsDocAnnotationsForVitestAndPlaywright()
        {
            var source = @"
import { describe, it, expect } from 'vitest';

describe('ToolTesterCard', () => {
  /**
   * @id UI-04
   * @category UI
   * @type positive
   * @description Dynamic form generation validates and casts schema input values
   */
  it('renders dynamic schema fields correctly', () => {
    expect(true).toBe(true);
  });

  /**
   * @id GUARD-02
   * @category GUARD
   * @type negative
   * @description Denied user role never exposes server API tokens in inspect modal
   */
  test('should hide secret tokens for guest users', async () => {
    expect(true).toBe(true);
  });
});
";

            var index = new CatalogIndex();
            var parser = new TypeScriptTestParser();
            parser.ParseSource("frontend/src/test/components/ToolTesterCard.test.tsx", source, index);

            Assert.Equal(2, index.Requirements.Count);

            var ui04 = index.Requirements["UI-04"];
            Assert.Equal("UI", ui04.Category);
            Assert.Equal(RequirementType.Positive, ui04.Type);
            Assert.Equal("Dynamic form generation validates and casts schema input values", ui04.Description);
            Assert.Single(ui04.Proofs);
            Assert.Equal("renders dynamic schema fields correctly", ui04.Proofs[0].TestName);

            var guard02 = index.Requirements["GUARD-02"];
            Assert.Equal("GUARD", guard02.Category);
            Assert.Equal(RequirementType.Negative, guard02.Type);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj --filter "FullyQualifiedName~TypeScriptTestParserTests"`
Expected: FAIL (`TypeScriptTestParser` not found)

- [ ] **Step 3: Implement `TypeScriptTestParser`**

```csharp
// scripts/CatalogGenerator/Parsers/TypeScriptTestParser.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using CatalogGenerator.Models;

namespace CatalogGenerator.Parsers
{
    public class TypeScriptTestParser
    {
        private static readonly Regex JsDocTestRegex = new Regex(
            @"/\*\*(?<jsdoc>[\s\S]*?)\*/\s*(?:it|test|test\.skip|it\.skip)\s*\(\s*(?:'|""|`)(?<name>[\s\S]*?)(?:'|""|`)",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        public void ParseDirectory(string directoryPath, CatalogIndex index, string suiteName)
        {
            if (!Directory.Exists(directoryPath)) return;

            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext != ".ts" && ext != ".tsx" && ext != ".js") continue;
                if (file.Contains("node_modules") || file.Contains(".next") || file.Contains("dist")) continue;

                var code = File.ReadAllText(file);
                ParseSource(file, code, index, suiteName);
            }
        }

        public void ParseSource(string filePath, string sourceCode, CatalogIndex index, string suiteName = "Frontend")
        {
            var lines = sourceCode.Split('\n');
            var matches = JsDocTestRegex.Matches(sourceCode);

            foreach (Match match in matches)
            {
                var jsdoc = match.Groups["jsdoc"].Value;
                var testName = match.Groups["name"].Value.Trim();

                var idMatch = Regex.Match(jsdoc, @"@id\s+([A-Za-z0-9\-_]+)");
                if (!idMatch.Success) continue;

                var id = idMatch.Groups[1].Value.Trim();

                var catMatch = Regex.Match(jsdoc, @"@category\s+([A-Za-z0-9\-_]+)");
                var category = catMatch.Success ? catMatch.Groups[1].Value.Trim() : (id.Contains('-') ? id.Substring(0, id.IndexOf('-')) : "UI");

                var typeMatch = Regex.Match(jsdoc, @"@type\s+([A-Za-z0-9\-_/]+)");
                var typeStr = typeMatch.Success ? typeMatch.Groups[1].Value.ToLowerInvariant() : "positive";
                var type = (typeStr.Contains("neg") || typeStr.Contains("guard")) ? RequirementType.Negative : RequirementType.Positive;

                var descMatch = Regex.Match(jsdoc, @"@desc(?:ription)?\s+([^\r\n*]+)");
                var desc = descMatch.Success ? descMatch.Groups[1].Value.Trim() : testName;

                // Find line number
                var matchIndex = match.Index;
                var charCount = 0;
                var lineNumber = 1;
                for (int i = 0; i < lines.Length; i++)
                {
                    charCount += lines[i].Length + 1;
                    if (charCount >= matchIndex)
                    {
                        lineNumber = i + 1;
                        break;
                    }
                }

                var proof = new TestCaseProof(
                    Suite: suiteName,
                    FilePath: filePath.Replace('\\', '/'),
                    LineNumber: lineNumber,
                    TestName: testName
                );

                index.AddOrMergeProof(id, category, type, desc, proof);
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj --filter "FullyQualifiedName~TypeScriptTestParserTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add scripts/CatalogGenerator/Parsers scripts/CatalogGenerator/Tests
git commit -m "feat(catalog): implement TypeScript and JSDoc parser for Vitest and Playwright"
```

---

### Task 4: Markdown & JSON Emitters and CLI Entrypoint

**Files:**
- Create: `scripts/CatalogGenerator/Emitters/MarkdownEmitter.cs`
- Create: `scripts/CatalogGenerator/Emitters/JsonEmitter.cs`
- Create: `scripts/CatalogGenerator/Program.cs`
- Test: `scripts/CatalogGenerator/Tests/EmitterTests.cs`

**Interfaces:**
- Consumes: `CatalogIndex`
- Produces: `MarkdownEmitter.Emit(CatalogIndex index)`, `JsonEmitter.Emit(CatalogIndex index)`

- [ ] **Step 1: Write failing test for Emitters**

```csharp
// scripts/CatalogGenerator/Tests/EmitterTests.cs
using Xunit;
using CatalogGenerator.Models;
using CatalogGenerator.Emitters;

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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj --filter "FullyQualifiedName~EmitterTests"`
Expected: FAIL

- [ ] **Step 3: Implement `MarkdownEmitter`, `JsonEmitter`, and `Program.cs`**

```csharp
// scripts/CatalogGenerator/Emitters/MarkdownEmitter.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using CatalogGenerator.Models;

namespace CatalogGenerator.Emitters
{
    public static class MarkdownEmitter
    {
        public static string Emit(CatalogIndex index)
        {
            var sb = new StringBuilder();
            var all = index.GetAll().ToList();
            var positive = all.Where(r => r.Type == RequirementType.Positive).ToList();
            var guardrails = all.Where(r => r.Type == RequirementType.Negative).ToList();
            var categories = all.GroupBy(r => r.Category).OrderBy(g => g.Key).ToList();

            sb.AppendLine("# Software Requirements Specification (SRS) & Test Verification Catalog");
            sb.AppendLine();
            sb.AppendLine("> **Automated Verification Document:** Generated via `dotnet run --project scripts/CatalogGenerator`");
            sb.AppendLine($"> **Catalog Statistics:** **{all.Count} Requirements Verified** across **{all.Sum(r => r.Proofs.Count)} Test Proofs** ({positive.Count} Functional Capabilities, {guardrails.Count} Safety Guardrails).");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## 1. System Taxonomy & Verification Summary");
            sb.AppendLine();
            sb.AppendLine("| Category | Domain | Total Requirements | Positive Features | Guardrails / Fail-Closed | Verification Proofs |");
            sb.AppendLine("| :--- | :--- | :---: | :---: | :---: | :---: |");

            foreach (var cat in categories)
            {
                var catTotal = cat.Count();
                var catPos = cat.Count(r => r.Type == RequirementType.Positive);
                var catNeg = cat.Count(r => r.Type == RequirementType.Negative);
                var catProofs = cat.Sum(r => r.Proofs.Count);
                sb.AppendLine($"| **`{cat.Key}`** | {GetCategoryTitle(cat.Key)} | **{catTotal}** | {catPos} | {catNeg} | {catProofs} proofs |");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## 2. Functional Requirements (\"What the Application DOES\")");
            sb.AppendLine();

            foreach (var req in positive)
            {
                sb.AppendLine($"### `[{req.Id}]` {req.Description}");
                sb.AppendLine($"* **Category:** `{req.Category}` ({GetCategoryTitle(req.Category)})");
                sb.AppendLine($"* **Type:** Positive Feature Capability");
                sb.AppendLine($"* **Verification Proofs ({req.Proofs.Count}):**");
                foreach (var proof in req.Proofs)
                {
                    sb.AppendLine($"  - [{proof.Suite}] [`{proof.FilePath}#L{proof.LineNumber}`](file:///{proof.FilePath}#L{proof.LineNumber}) (`{proof.TestName}`)");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 3. Boundary & Guardrail Invariants (\"What the Application DOES NOT DO\")");
            sb.AppendLine();
            sb.AppendLine("> [!IMPORTANT]");
            sb.AppendLine("> The following guardrails define strict security boundaries, fail-closed fault invariants, and forbidden application states.");
            sb.AppendLine();

            foreach (var req in guardrails)
            {
                sb.AppendLine($"### `[{req.Id}]` {req.Description}");
                sb.AppendLine($"* **Category:** `{req.Category}` ({GetCategoryTitle(req.Category)})");
                sb.AppendLine($"* **Type:** Negative / Safety Guardrail (Fail-Closed)");
                sb.AppendLine($"* **Verification Proofs ({req.Proofs.Count}):**");
                foreach (var proof in req.Proofs)
                {
                    sb.AppendLine($"  - [{proof.Suite}] [`{proof.FilePath}#L{proof.LineNumber}`](file:///{proof.FilePath}#L{proof.LineNumber}) (`{proof.TestName}`)");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 4. Complete Verification Traceability Matrix");
            sb.AppendLine();
            sb.AppendLine("| Requirement ID | Type | Category | Description | Primary Proof | Suite |");
            sb.AppendLine("| :--- | :---: | :--- | :--- | :--- | :--- |");

            foreach (var req in all)
            {
                var p = req.Proofs.FirstOrDefault();
                var proofStr = p != null ? $"[`{Path.GetFileName(p.FilePath)}:L{p.LineNumber}`](file:///{p.FilePath}#L{p.LineNumber})" : "N/A";
                var suiteStr = p != null ? p.Suite : "N/A";
                var typeStr = req.Type == RequirementType.Positive ? "Positive" : "**Guardrail**";
                sb.AppendLine($"| `{req.Id}` | {typeStr} | `{req.Category}` | {req.Description} | {proofStr} | {suiteStr} |");
            }

            return sb.ToString();
        }

        private static string GetCategoryTitle(string cat) => cat.ToUpperInvariant() switch
        {
            "AUTH" => "Authentication, RBAC & Identity",
            "MCP" => "Model Context Protocol Engine & Tool Routing",
            "TRANS" => "Transports (SSE, HTTP, STDIO, Proxy)",
            "SEC" => "Secrets Providers & Encryption",
            "DB" => "Multi-Database Persistence & Migrations",
            "UI" => "Dashboard, Test Bench & Settings UI",
            "GUARD" => "Universal Safety & Fail-Closed Guardrails",
            _ => cat
        };
    }
}
```

```csharp
// scripts/CatalogGenerator/Emitters/JsonEmitter.cs
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
```

```csharp
// scripts/CatalogGenerator/Program.cs
using System;
using System.IO;
using CatalogGenerator.Models;
using CatalogGenerator.Parsers;
using CatalogGenerator.Emitters;

namespace CatalogGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine(" MCP Router Software Requirements Catalog Engine ");
            Console.WriteLine("=================================================");

            var rootDir = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(rootDir, "mcp-router.csproj")) && Directory.GetParent(rootDir) != null)
            {
                rootDir = Directory.GetParent(rootDir)!.FullName;
            }

            Console.WriteLine($"[INFO] Repository Root: {rootDir}");

            var verifyOnly = args.Contains("--verify-only") || args.Contains("--verify");
            var index = new CatalogIndex();

            // 1. Parse C# tests
            var csParser = new RoslynCSharpParser();
            var csTestDir = Path.Combine(rootDir, "McpRouter.Tests");
            if (Directory.Exists(csTestDir))
            {
                Console.WriteLine($"[INFO] Scanning C# xUnit tests in {csTestDir}...");
                csParser.ParseDirectory(csTestDir, index);
            }

            // 2. Parse Vitest tests
            var tsParser = new TypeScriptTestParser();
            var vitestDir = Path.Combine(rootDir, "frontend", "src", "test");
            if (Directory.Exists(vitestDir))
            {
                Console.WriteLine($"[INFO] Scanning Frontend Vitest tests in {vitestDir}...");
                tsParser.ParseDirectory(vitestDir, index, "Frontend Vitest");
            }

            // 3. Parse Playwright tests
            var playwrightDir = Path.Combine(rootDir, "frontend", "e2e");
            if (Directory.Exists(playwrightDir))
            {
                Console.WriteLine($"[INFO] Scanning Playwright E2E tests in {playwrightDir}...");
                tsParser.ParseDirectory(playwrightDir, index, "Playwright E2E");
            }

            var all = index.GetAll().ToList();
            Console.WriteLine($"[SUCCESS] Discovered {all.Count} requirements across {all.Sum(r => r.Proofs.Count)} proofs.");

            var mdOutput = MarkdownEmitter.Emit(index);
            var jsonOutput = JsonEmitter.Emit(index);

            var mdPath = Path.Combine(rootDir, "docs", "software-requirements-and-test-catalog.md");
            var jsonPath = Path.Combine(rootDir, "docs", "requirements-catalog.json");

            if (verifyOnly)
            {
                if (!File.Exists(mdPath) || !File.Exists(jsonPath))
                {
                    Console.Error.WriteLine("[ERROR] Verification failed: Requirement catalog files are missing!");
                    return 1;
                }

                var existingMd = File.ReadAllText(mdPath);
                if (existingMd.Trim() != mdOutput.Trim())
                {
                    Console.Error.WriteLine("[ERROR] Verification failed: docs/software-requirements-and-test-catalog.md is out of date! Run without --verify to update.");
                    return 1;
                }

                Console.WriteLine("[SUCCESS] Requirement catalog is completely up to date!");
                return 0;
            }

            Directory.CreateDirectory(Path.Combine(rootDir, "docs"));
            File.WriteAllText(mdPath, mdOutput);
            File.WriteAllText(jsonPath, jsonOutput);

            Console.WriteLine($"[UPDATED] Markdown catalog written to: {mdPath}");
            Console.WriteLine($"[UPDATED] JSON catalog written to:     {jsonPath}");

            return 0;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test scripts/CatalogGenerator/CatalogGenerator.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add scripts/CatalogGenerator
git commit -m "feat(catalog): implement Markdown and JSON catalog emitters and CLI entrypoint"
```

---

### Task 5: Annotate Key Test Suites Across Backend, Frontend & E2E

**Files:**
- Modify: `McpRouter.Tests/PairwiseIntegrationMatrixTests.cs`
- Modify: `McpRouter.Tests/AdminPolicySidOnlyTests.cs`
- Modify: `McpRouter.Tests/CategoryScopedAppKeysTests.cs`
- Modify: `McpRouter.Tests/SseTransportTests.cs`
- Modify: `McpRouter.Tests/HttpTransportTests.cs`
- Modify: `McpRouter.Tests/StdioTransportTests.cs`
- Modify: `McpRouter.Tests/VaultAppRoleAndRenewalTests.cs`
- Modify: `McpRouter.Tests/DatabaseSchemaUpgradeAndContractTests.cs`
- Modify: `frontend/src/test/components/ToolTesterCard.test.tsx`
- Modify: `frontend/src/test/components/ServerInspectModal.test.tsx`
- Modify: `frontend/src/test/components/DashboardView.test.tsx`
- Modify: `frontend/e2e/dashboard.spec.ts`
- Modify: `frontend/e2e/rbac-enforcement-flow.spec.ts`
- Modify: `frontend/e2e/multi-user-matrix.spec.ts`

- [ ] **Step 1: Annotate Backend C# test cases with `[Requirement]` attributes**
  - Add `[Requirement("AUTH-01", ...)]` to `AdminPolicySidOnlyTests.cs`
  - Add `[Requirement("AUTH-02", ...)]` to `CategoryScopedAppKeysTests.cs`
  - Add `[Requirement("GUARD-01", ...)]` to `PairwiseIntegrationMatrixTests.cs` for corrupted/expired scope fail-closed tests
  - Add `[Requirement("TRANS-01", ...)]`, `[Requirement("TRANS-02", ...)]`, `[Requirement("TRANS-03", ...)]` to transport test files
  - Add `[Requirement("SEC-01", ...)]` to `VaultAppRoleAndRenewalTests.cs`
  - Add `[Requirement("DB-01", ...)]` to `DatabaseSchemaUpgradeAndContractTests.cs`

- [ ] **Step 2: Annotate Frontend Vitest and Playwright tests with JSDoc tags**
  - Add `UI-01`, `UI-02`, `UI-03`, `UI-04`, `GUARD-02`, `GUARD-03` JSDoc annotations to frontend tests.

- [ ] **Step 3: Run test suite to verify tests pass and traits work**

Run: `CI=true dotnet test McpRouter.slnx`
Expected: PASS
Run: `cd frontend && npm run test`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add McpRouter.Tests frontend/src/test frontend/e2e
git commit -m "test(catalog): annotate backend and frontend test suites with requirement IDs"
```

---

### Task 6: Documentation, Test Guide & CI Script Integration

**Files:**
- Create: `docs/test-catalog-guide.md`
- Create: `docs/software-requirements-and-test-catalog.md` (Generated)
- Create: `docs/requirements-catalog.json` (Generated)
- Modify: `frontend/package.json`
- Modify: `README.md`
- Modify: `docs/features-guide.md`

- [ ] **Step 1: Execute Catalog Generator to emit fresh documentation**

Run: `dotnet run --project scripts/CatalogGenerator`
Expected: Created `docs/software-requirements-and-test-catalog.md` and `docs/requirements-catalog.json` with 0 errors.

- [ ] **Step 2: Add scripts to `frontend/package.json`**

```json
"docs:catalog": "dotnet run --project ../scripts/CatalogGenerator",
"docs:catalog:verify": "dotnet run --project ../scripts/CatalogGenerator -- --verify-only"
```

- [ ] **Step 3: Create `docs/test-catalog-guide.md`**

Write a complete developer and AI agent guide explaining the taxonomy, requirement attributes, JSDoc annotations, and how to execute the generator.

- [ ] **Step 4: Commit**

```bash
git add docs/ frontend/package.json README.md
git commit -m "docs(catalog): generate software requirements catalog and developer guide"
```

---

### Task 7: Agent Guidelines & Mandatory Version Bump to v4.15.0

**Files:**
- Modify: `AGENTS.md`
- Modify: `.agents/GEMINI.md`
- Modify: `mcp-router.csproj`
- Modify: `frontend/package.json`
- Modify: `frontend/src/stores/useUserStore.ts`
- Modify: `CHANGELOG.md`
- Modify: `README.md`

- [ ] **Step 1: Update `AGENTS.md` and `.agents/GEMINI.md` with Mandatory Test Requirement Annotations Rule**
- [ ] **Step 2: Update version `4.15.0` simultaneously across the 5 mandatory files**
- [ ] **Step 3: Run full verification suite**

Run: `dotnet test McpRouter.slnx`
Run: `dotnet run --project scripts/CatalogGenerator -- --verify-only`
Run: `cd frontend && npm run lint && npm run build`

- [ ] **Step 4: Commit**

```bash
git add AGENTS.md .agents/GEMINI.md mcp-router.csproj frontend/package.json frontend/src/stores/useUserStore.ts CHANGELOG.md README.md
git commit -m "release: bump version to v4.15.0 and establish test requirement agent rules"
```

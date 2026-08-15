using System.IO;
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
import { describe, it, expect, test } from 'vitest';

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

  /**
   * @id E2E-01
   * @type negative
   */
  test.skip(`handles server timeout gracefully`, async () => {
    expect(true).toBe(true);
  });
});
";

            var index = new CatalogIndex();
            var parser = new TypeScriptTestParser();
            parser.ParseSource("frontend/src/test/components/ToolTesterCard.test.tsx", source, index, "Frontend Unit Tests");

            Assert.Equal(3, index.Requirements.Count);

            var ui04 = index.Requirements["UI-04"];
            Assert.Equal("UI", ui04.Category);
            Assert.Equal(RequirementType.Positive, ui04.Type);
            Assert.Equal("Dynamic form generation validates and casts schema input values", ui04.Description);
            Assert.Single(ui04.Proofs);
            Assert.Equal("renders dynamic schema fields correctly", ui04.Proofs[0].TestName);
            Assert.Equal("Frontend Unit Tests", ui04.Proofs[0].Suite);
            Assert.Equal("frontend/src/test/components/ToolTesterCard.test.tsx", ui04.Proofs[0].FilePath);

            var guard02 = index.Requirements["GUARD-02"];
            Assert.Equal("GUARD", guard02.Category);
            Assert.Equal(RequirementType.Negative, guard02.Type);
            Assert.Equal("Denied user role never exposes server API tokens in inspect modal", guard02.Description);

            var e2e01 = index.Requirements["E2E-01"];
            Assert.Equal("E2E", e2e01.Category); // Derived from prefix
            Assert.Equal(RequirementType.Negative, e2e01.Type);
            Assert.Equal("handles server timeout gracefully", e2e01.Description); // Fallback to test name
            Assert.Equal("handles server timeout gracefully", e2e01.Proofs[0].TestName);
        }

        [Fact]
        public void ParseDirectory_ScansFilesAndSkipsExcludedFolders()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ts_parser_test_" + Path.GetRandomFileName());
            var normalDir = Path.Combine(tempDir, "src", "tests");
            var nodeModulesDir = Path.Combine(tempDir, "node_modules", "pkg");
            Directory.CreateDirectory(normalDir);
            Directory.CreateDirectory(nodeModulesDir);

            try
            {
                var validTest = @"
/**
 * @id TEST-01
 * @category TEST
 * @type positive
 * @description Valid test
 */
it(""runs ok"", () => {});
";
                var ignoredTest = @"
/**
 * @id IGNORE-01
 * @category IGNORE
 * @type positive
 * @description Should be ignored
 */
it(""ignored"", () => {});
";
                File.WriteAllText(Path.Combine(normalDir, "sample.test.ts"), validTest);
                File.WriteAllText(Path.Combine(nodeModulesDir, "dep.test.ts"), ignoredTest);

                var index = new CatalogIndex();
                var parser = new TypeScriptTestParser();
                parser.ParseDirectory(tempDir, index, "Temp Suite");

                Assert.True(index.Requirements.ContainsKey("TEST-01"));
                Assert.False(index.Requirements.ContainsKey("IGNORE-01"));
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
        public void ParseTypeScript_AcceptsRequirementAndReqTags_WithVariousTypeFormats()
        {
            var source = @"
describe('NewTagFeatures', () => {
  /**
   * @requirement UI-05
   * @category UI
   * @type PositiveFeature
   * @description Dynamic form generation validates and casts schema input values
   */
  it('renders and validates form controls', () => {
    expect(true).toBe(true);
  });

  /**
   * @req GUARD-09
   * @category GUARD
   * @type FailClosedGuardrail
   * @desc API tokens never leak to unauthorized callers
   */
  test('fails closed on token access', async () => {
    expect(true).toBe(true);
  });
});
";

            var index = new CatalogIndex();
            var parser = new TypeScriptTestParser();
            parser.ParseSource("frontend/src/test/components/NewTags.test.tsx", source, index, "Frontend Unit Tests");

            Assert.Equal(2, index.Requirements.Count);

            var ui05 = index.Requirements["UI-05"];
            Assert.Equal("UI", ui05.Category);
            Assert.Equal(RequirementType.Positive, ui05.Type);
            Assert.Equal("Dynamic form generation validates and casts schema input values", ui05.Description);
            Assert.Single(ui05.Proofs);
            Assert.Equal("renders and validates form controls", ui05.Proofs[0].TestName);

            var guard09 = index.Requirements["GUARD-09"];
            Assert.Equal("GUARD", guard09.Category);
            Assert.Equal(RequirementType.Negative, guard09.Type);
            Assert.Equal("API tokens never leak to unauthorized callers", guard09.Description);
            Assert.Single(guard09.Proofs);
            Assert.Equal("fails closed on token access", guard09.Proofs[0].TestName);
        }
    }
}

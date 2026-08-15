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
                var normalized = file.Replace('\\', '/');
                // Skip bin, obj, AssemblyInfo, GlobalUsings
                if (normalized.Contains("/bin/") ||
                    normalized.Contains("/obj/") ||
                    normalized.EndsWith("/GlobalUsings.cs") ||
                    normalized.EndsWith("GlobalUsings.cs"))
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

                    var positionalArgs = attr.ArgumentList.Arguments.Where(a => a.NameEquals == null).ToList();
                    if (positionalArgs.Count == 0) continue;

                    var idArg = positionalArgs[0].Expression.ToString().Trim('"');
                    if (string.IsNullOrWhiteSpace(idArg)) continue;

                    var descArg = positionalArgs.Count > 1 
                        ? positionalArgs[1].Expression.ToString().Trim('"') 
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

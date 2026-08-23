using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpRouter.Tests.Attributes;
using Xunit;

namespace McpRouter.Tests
{
    public class AdminAutomationSkillTests
    {
        private static string GetRepoRootDir()
        {
            var rootDir = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(rootDir, "mcp-router.csproj")) && Directory.GetParent(rootDir) != null)
            {
                rootDir = Directory.GetParent(rootDir)!.FullName;
            }
            return rootDir;
        }

        [Fact]
        [Requirement("MCP-ADMIN-SKILL-FRONTMATTER", "MCP", RequirementType.Positive, "mcp-router-admin skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters")]
        public void Skill_Frontmatter_IsValidAndWithinCharacterLimit()
        {
            var root = GetRepoRootDir();
            var skillPath = Path.Combine(root, "skills", "mcp-router-admin", "SKILL.md");

            Assert.True(File.Exists(skillPath), $"Expected skill file at {skillPath} to exist.");

            var content = File.ReadAllText(skillPath);
            Assert.StartsWith("---", content.TrimStart());

            var match = Regex.Match(content, @"^---\r?\n(.*?)\r?\n---", RegexOptions.Singleline);
            Assert.True(match.Success, "Frontmatter must be enclosed in opening and closing '---' markers.");

            var frontmatter = match.Groups[1].Value.Trim();
            Assert.True(frontmatter.Length < 1024, $"Frontmatter length ({frontmatter.Length}) must be under 1024 characters.");

            Assert.Matches(@"name:\s*mcp-router-admin", frontmatter);

            var descMatch = Regex.Match(frontmatter, @"description:\s*(.+)", RegexOptions.Singleline);
            Assert.True(descMatch.Success, "Frontmatter must contain a description field.");
            var description = descMatch.Groups[1].Value.Trim();
            Assert.StartsWith("Use when", description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [Requirement("MCP-ADMIN-SKILL-WORKFLOW", "MCP", RequirementType.Positive, "mcp-router-admin skill contains all 7 administration phases including diagnostics, secrets, auth providers, RBAC/group mappings, settings/embeddings, servers/clients, and live tool verification")]
        public void Skill_ContainsAllRequiredPhasesAndProviderCookbooks()
        {
            var root = GetRepoRootDir();
            var skillPath = Path.Combine(root, "skills", "mcp-router-admin", "SKILL.md");
            Assert.True(File.Exists(skillPath));

            var content = File.ReadAllText(skillPath);

            // Blank-slate Safe Defaults
            Assert.Contains("Safe Defaults", content);
            Assert.Contains("mcp-global-admin-default-cli-key-99", content);
            Assert.Contains("ROUTER_MASTER_KEY", content);

            // Phase 1: Gateway Diagnostics
            Assert.Contains("Phase 1", content);
            Assert.Contains("diagnostics", content);
            Assert.Contains("manage_system", content);

            // Phase 2: Secret Provider Configuration
            Assert.Contains("Phase 2", content);
            Assert.Contains("Secret Provider", content);
            Assert.Contains("HashiCorpVault", content);
            Assert.Contains("test_vault", content);

            // Phase 3: Authentication Provider Configuration
            Assert.Contains("Phase 3", content);
            Assert.Contains("Authentik", content);
            Assert.Contains("Keycloak", content);
            Assert.Contains("EntraID", content);
            Assert.Contains("ActiveDirectory", content);
            Assert.Contains("test_ldap", content);

            // Phase 4: RBAC & Group Mappings
            Assert.Contains("Phase 4", content);
            Assert.Contains("manage_group_mappings", content);
            Assert.Contains("manage_policies", content);

            // Phase 5: Dynamic Embeddings & Settings
            Assert.Contains("Phase 5", content);
            Assert.Contains("manage_settings", content);
            Assert.Contains("OpenAI", content);
            Assert.Contains("Ollama", content);

            // Phase 6: Backend Servers & Client AppKeys
            Assert.Contains("Phase 6", content);
            Assert.Contains("manage_servers", content);
            Assert.Contains("manage_appkeys", content);

            // Phase 7: Verification & Diagnostics
            Assert.Contains("Phase 7", content);
            Assert.Contains("test_tool_call", content);
            Assert.Contains("query_audit", content);
        }

        [Fact]
        [Requirement("MCP-ADMIN-SKILL-TEMPLATES", "MCP", RequirementType.Positive, "All mcp-router-admin scaffold templates exist, are non-empty, and contain valid JSON or scripts for Authentik, Keycloak, Entra, ActiveDirectory, Cloudflare, Vault, Embeddings, Docker, and shell automation")]
        public void Templates_AllExistAndAreValidJsonOrScripts()
        {
            var root = GetRepoRootDir();
            var templatesDir = Path.Combine(root, "skills", "mcp-router-admin", "templates");
            Assert.True(Directory.Exists(templatesDir), $"Templates directory {templatesDir} must exist.");

            var jsonTemplateFiles = new[]
            {
                "auth-authentik-forwardauth.json",
                "auth-keycloak-oidc.json",
                "auth-entra-azure-ad.json",
                "auth-activedirectory-ldap.json",
                "auth-cloudflare-access.json",
                "secret-vault-token.json",
                "secret-vault-approle.json",
                "settings-openai-embeddings.json",
                "settings-ollama-embeddings.json",
                "server-docker-mcp.json"
            };

            foreach (var file in jsonTemplateFiles)
            {
                var filePath = Path.Combine(templatesDir, file);
                Assert.True(File.Exists(filePath), $"Expected template file {file} to exist.");
                var content = File.ReadAllText(filePath);
                Assert.NotEmpty(content);

                // Verify valid JSON format
                using var jsonDoc = JsonDocument.Parse(content);
                Assert.NotNull(jsonDoc);
            }

            // Shell & PowerShell automation scripts
            var bashScript = Path.Combine(templatesDir, "automate-setup.sh");
            Assert.True(File.Exists(bashScript));
            Assert.Contains("mcp-global-admin-default-cli-key-99", File.ReadAllText(bashScript));

            var psScript = Path.Combine(templatesDir, "automate-setup.ps1");
            Assert.True(File.Exists(psScript));
            Assert.Contains("mcp-global-admin-default-cli-key-99", File.ReadAllText(psScript));
        }

        [Fact]
        [Requirement("MCP-ADMIN-SKILL-MIRROR", "MCP", RequirementType.Positive, "mcp-router-admin skill files and templates are identically mirrored between skills/ and .agents/skills/ directories")]
        public void Skill_MirroredInAgentsDirectory()
        {
            var root = GetRepoRootDir();
            var sourceSkill = Path.Combine(root, "skills", "mcp-router-admin", "SKILL.md");
            var targetSkill = Path.Combine(root, ".agents", "skills", "mcp-router-admin", "SKILL.md");

            Assert.True(File.Exists(sourceSkill), "Source SKILL.md must exist.");
            Assert.True(File.Exists(targetSkill), "Mirrored SKILL.md in .agents must exist.");

            var sourceContent = File.ReadAllText(sourceSkill);
            var targetContent = File.ReadAllText(targetSkill);
            Assert.Equal(sourceContent, targetContent);

            var sourceTemplatesDir = Path.Combine(root, "skills", "mcp-router-admin", "templates");
            var targetTemplatesDir = Path.Combine(root, ".agents", "skills", "mcp-router-admin", "templates");

            Assert.True(Directory.Exists(targetTemplatesDir), "Mirrored templates directory in .agents must exist.");

            foreach (var file in Directory.GetFiles(sourceTemplatesDir))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetTemplatesDir, fileName);
                Assert.True(File.Exists(targetFile), $"Expected mirrored template {fileName} to exist in .agents/skills/mcp-router-admin/templates/");
                Assert.Equal(File.ReadAllText(file), File.ReadAllText(targetFile));
            }
        }
    }
}

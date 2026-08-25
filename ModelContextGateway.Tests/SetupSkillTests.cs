using System.Text.Json;
using System.Text.RegularExpressions;

namespace ModelContextGateway.Tests
{
    public class SetupSkillTests
    {
        private static string GetRepoRootDir()
        {
            var rootDir = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(rootDir, "ModelContextGateway.csproj")) && Directory.GetParent(rootDir) != null)
            {
                rootDir = Directory.GetParent(rootDir)!.FullName;
            }
            return rootDir;
        }

        [Fact]
        [Requirement("DOC-SETUP-SKILL-FRONTMATTER", "DOC", RequirementType.Positive, "mcg-setup skill frontmatter is valid YAML, specifies name, description starting with 'Use when...', and length is under 1024 characters")]
        public void Skill_Frontmatter_IsValidAndWithinCharacterLimit()
        {
            var root = GetRepoRootDir();
            var skillPath = Path.Combine(root, "skills", "mcg-setup", "SKILL.md");

            Assert.True(File.Exists(skillPath), $"Expected skill file at {skillPath} to exist.");

            var content = File.ReadAllText(skillPath);
            Assert.StartsWith("---", content.TrimStart());

            var match = Regex.Match(content, @"^---\r?\n(.*?)\r?\n---", RegexOptions.Singleline);
            Assert.True(match.Success, "Frontmatter must be enclosed in opening and closing '---' markers.");

            var frontmatter = match.Groups[1].Value.Trim();
            Assert.True(frontmatter.Length < 1024, $"Frontmatter length ({frontmatter.Length}) must be under 1024 characters.");

            Assert.Matches(@"name:\s*mcg-setup", frontmatter);

            var descMatch = Regex.Match(frontmatter, @"description:\s*(.+)", RegexOptions.Singleline);
            Assert.True(descMatch.Success, "Frontmatter must contain a description field.");
            var description = descMatch.Groups[1].Value.Trim();
            Assert.StartsWith("Use when", description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [Requirement("DOC-SETUP-SKILL-WORKFLOW", "DOC", RequirementType.Positive, "mcg-setup skill contains all 6 required setup phases including environment probing, hosting platforms, env vs UI trade-offs, identity/network topology, artifact generation, and health/client configuration")]
        public void Skill_ContainsAllRequiredPhasesAndComparisons()
        {
            var root = GetRepoRootDir();
            var skillPath = Path.Combine(root, "skills", "mcg-setup", "SKILL.md");
            Assert.True(File.Exists(skillPath));

            var content = File.ReadAllText(skillPath);

            // Phase 1: Automated Environment Probing
            Assert.Contains("Phase 1", content);
            Assert.Contains("Environment Probing", content);
            Assert.Contains("/var/run/docker.sock", content);
            Assert.Contains("VAULT_ADDR", content);
            Assert.Contains("USERDNSDOMAIN", content);

            // Phase 2: Hosting Platform Selection
            Assert.Contains("Phase 2", content);
            Assert.Contains("Hosting Platform", content);
            Assert.Contains("Docker", content);
            Assert.Contains("IIS", content);

            // Phase 3: Configuration Paradigm (Env vs UI & Database)
            Assert.Contains("Phase 3", content);
            Assert.Contains("Configuration Paradigm", content);
            Assert.Contains("Environment Variables", content);
            Assert.Contains("Web UI & Database", content);

            // Phase 4: Identity & Network Topology
            Assert.Contains("Phase 4", content);
            Assert.Contains("Identity & Network Topology", content);
            Assert.Contains("Standalone Mode", content);
            Assert.Contains("Enterprise Mode", content);
            Assert.Contains("Admin:StandaloneAllowedNetworks", content);

            // Phase 5: Artifact Generation & Secrets Scaffolding
            Assert.Contains("Phase 5", content);
            Assert.Contains("Artifact Generation", content);
            Assert.Contains("MCG_MASTER_KEY", content);
            Assert.Contains("openssl rand -base64 32", content);

            // Phase 6: Health Verification & Client Setup
            Assert.Contains("Phase 6", content);
            Assert.Contains("Health Verification", content);
            Assert.Contains("/health", content);
            Assert.Contains("/sse", content);
            Assert.Contains("Claude Desktop", content);
            Assert.Contains("Cursor", content);
            Assert.Contains("Cline", content);
            Assert.Contains("Windsurf", content);
            Assert.Contains("Admin MCP Server", content);
        }

        [Fact]
        [Requirement("DOC-SETUP-SKILL-TEMPLATES", "DOC", RequirementType.Positive, "All scaffold templates exist, are non-empty, and contain required directives such as responseBufferLimit, MCG_MASTER_KEY, and ghcr.io/spelech/model-context-gateway")]
        public void Templates_AreValidAndContainRequiredDirectives()
        {
            var root = GetRepoRootDir();
            var templatesDir = Path.Combine(root, "skills", "mcg-setup", "templates");
            Assert.True(Directory.Exists(templatesDir), $"Templates directory {templatesDir} must exist.");

            // 1. docker-compose.yml
            var composePath = Path.Combine(templatesDir, "docker-compose.yml");
            Assert.True(File.Exists(composePath));
            var composeContent = File.ReadAllText(composePath);
            Assert.NotEmpty(composeContent);
            Assert.Contains("ghcr.io/spelech/model-context-gateway", composeContent);
            Assert.Contains("MCG_MASTER_KEY", composeContent);
            Assert.Contains("DB_PROVIDER", composeContent);
            Assert.Contains("Admin__StandaloneAllowedNetworks", composeContent);
            Assert.Contains("/var/run/docker.sock", composeContent);

            // 2. web.config
            var webConfigPath = Path.Combine(templatesDir, "web.config");
            Assert.True(File.Exists(webConfigPath));
            var webConfigContent = File.ReadAllText(webConfigPath);
            Assert.NotEmpty(webConfigContent);
            Assert.Contains("responseBufferLimit", webConfigContent);
            Assert.Contains("value=\"0\"", webConfigContent);
            Assert.Contains("AspNetCoreModuleV2", webConfigContent);
            Assert.Contains("hostingModel=\"inprocess\"", webConfigContent);

            // 3. .env.example
            var envPath = Path.Combine(templatesDir, ".env.example");
            Assert.True(File.Exists(envPath));
            var envContent = File.ReadAllText(envPath);
            Assert.NotEmpty(envContent);
            Assert.Contains("MCG_MASTER_KEY", envContent);
            Assert.Contains("DB_PROVIDER", envContent);
            Assert.Contains("STANDALONE_ALLOWED_NETWORK", envContent);
            Assert.Contains("CORS_ALLOWED_ORIGINS", envContent);

            // 4. appsettings.Production.json.example
            var appsettingsPath = Path.Combine(templatesDir, "appsettings.Production.json.example");
            Assert.True(File.Exists(appsettingsPath));
            var appsettingsContent = File.ReadAllText(appsettingsPath);
            Assert.NotEmpty(appsettingsContent);

            using var jsonDoc = JsonDocument.Parse(appsettingsContent);
            var rootElem = jsonDoc.RootElement;
            Assert.True(rootElem.TryGetProperty("DB_PROVIDER", out _));
            Assert.True(rootElem.TryGetProperty("MCG_MASTER_KEY", out _));
            Assert.True(rootElem.TryGetProperty("Admin", out var adminElem));
            Assert.True(adminElem.TryGetProperty("StandaloneAllowedNetworks", out var netElem));
            Assert.Equal(JsonValueKind.Array, netElem.ValueKind);
        }

        [Fact]
        [Requirement("DOC-SETUP-SKILL-MIRROR", "DOC", RequirementType.Positive, "The mcg-setup skill and templates are mirrored 1:1 in .agents/skills/mcg-setup/")]
        public void Skill_MirroredInAgentsDirectory()
        {
            var root = GetRepoRootDir();
            var primarySkillDir = Path.Combine(root, "skills", "mcg-setup");
            var mirroredSkillDir = Path.Combine(root, ".agents", "skills", "mcg-setup");

            Assert.True(Directory.Exists(mirroredSkillDir), $"Mirrored skill directory {mirroredSkillDir} must exist.");

            // Verify SKILL.md mirror
            var primarySkillFile = Path.Combine(primarySkillDir, "SKILL.md");
            var mirroredSkillFile = Path.Combine(mirroredSkillDir, "SKILL.md");
            Assert.True(File.Exists(mirroredSkillFile), $"Mirrored SKILL.md must exist at {mirroredSkillFile}.");
            Assert.Equal(File.ReadAllText(primarySkillFile), File.ReadAllText(mirroredSkillFile));

            // Verify all template files mirror
            var templateFiles = new[]
            {
                "docker-compose.yml",
                "web.config",
                ".env.example",
                "appsettings.Production.json.example"
            };

            foreach (var template in templateFiles)
            {
                var primaryFile = Path.Combine(primarySkillDir, "templates", template);
                var mirroredFile = Path.Combine(mirroredSkillDir, "templates", template);

                Assert.True(File.Exists(mirroredFile), $"Mirrored template {mirroredFile} must exist.");
                Assert.Equal(File.ReadAllText(primaryFile), File.ReadAllText(mirroredFile));
            }
        }
    }
}

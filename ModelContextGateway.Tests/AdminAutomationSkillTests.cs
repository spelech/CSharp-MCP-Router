using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ModelContextGateway.Tests
{
    public class AdminAutomationSkillTests
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
            Assert.Contains("mcp-adm-", content);
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
            Assert.Contains("mcp-adm-", File.ReadAllText(bashScript));

            var psScript = Path.Combine(templatesDir, "automate-setup.ps1");
            Assert.True(File.Exists(psScript));
            Assert.Contains("mcp-adm-", File.ReadAllText(psScript));
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

        [Fact]
        [Requirement("MCP-ADMIN-SKILL-E2E-PROVISIONING", "MCP", RequirementType.Positive, "Admin automation templates and JSON-RPC tool calls successfully provision a blank-slate gateway instance end-to-end via HTTP /admin/message.")]
        public async Task EndToEnd_BlankSlateProvisioning_ConfiguresAllEntitiesViaAdminTools()
        {
            var root = GetRepoRootDir();
            var templatesDir = Path.Combine(root, "skills", "mcp-router-admin", "templates");

            var tempDbFile = Path.Combine(Path.GetTempPath(), $"mcp_admin_auto_{Guid.NewGuid():N}.db");
            try
            {
                using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { "ConnectionStrings:Sqlite", $"Data Source={tempDbFile}" },
                            { "DB_ENCRYPTION_KEY", "TestMasterSecretKey123456789012345678901234" },
                            { "Admin:StandaloneAllowedNetworks:0", "127.0.0.1" },
                            { "Admin:StandaloneAllowedNetworks:1", "::1" }
                        });
                    });
                });

                var client = factory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer mcp-global-admin-default-cli-key-99");

                async Task<JsonDocument> SendToolCallAsync(string toolName, object arguments)
                {
                    var payload = new
                    {
                        jsonrpc = "2.0",
                        id = Guid.NewGuid().ToString("N"),
                        method = "tools/call",
                        @params = new
                        {
                            name = toolName,
                            arguments
                        }
                    };

                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/admin", content);
                    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(responseBody);
                    Assert.True(!doc.RootElement.TryGetProperty("error", out var err) || err.ValueKind == JsonValueKind.Null, $"Tool call '{toolName}' returned JSON-RPC error: {responseBody}");
                    return doc;
                }

                // 1. Diagnostics Probe
                var diagDoc = await SendToolCallAsync("manage_system", new { action = "diagnostics" });
                Assert.True(diagDoc.RootElement.TryGetProperty("result", out var diagResult));
                Assert.Contains("activeSessions", diagResult.GetRawText());

                // 2. Provision Authentik Forward-Auth Provider
                var authentikRaw = File.ReadAllText(Path.Combine(templatesDir, "auth-authentik-forwardauth.json"));
                using var authDoc = JsonDocument.Parse(authentikRaw);
                var authElem = authDoc.RootElement;
                await SendToolCallAsync("manage_providers", new
                {
                    action = "save_auth",
                    providerName = authElem.GetProperty("providerName").GetString(),
                    displayName = authElem.GetProperty("displayName").GetString(),
                    userHeader = authElem.GetProperty("userHeader").GetString(),
                    groupsHeader = authElem.GetProperty("groupsHeader").GetString(),
                    isEnabled = authElem.GetProperty("isEnabled").GetBoolean(),
                    configJson = authElem.GetProperty("configJson").GetRawText()
                });

                // 3. Provision Vault AppRole Secret Provider
                var vaultRaw = File.ReadAllText(Path.Combine(templatesDir, "secret-vault-approle.json"));
                using var vaultDoc = JsonDocument.Parse(vaultRaw);
                var vaultElem = vaultDoc.RootElement;
                await SendToolCallAsync("manage_providers", new
                {
                    action = "save_secret",
                    providerName = vaultElem.GetProperty("providerName").GetString(),
                    displayName = vaultElem.GetProperty("displayName").GetString(),
                    isEnabled = vaultElem.GetProperty("isEnabled").GetBoolean(),
                    configJson = vaultElem.GetProperty("configJson").GetRawText()
                });

                // 4. Update Gateway Settings & OpenAI Embeddings
                var settingsRaw = File.ReadAllText(Path.Combine(templatesDir, "settings-openai-embeddings.json"));
                using var settingsDoc = JsonDocument.Parse(settingsRaw);
                var setElem = settingsDoc.RootElement;
                await SendToolCallAsync("manage_settings", new
                {
                    action = "update",
                    dashboardTitle = setElem.GetProperty("dashboardTitle").GetString(),
                    dashboardIcon = setElem.GetProperty("dashboardIcon").GetString(),
                    embeddingProvider = setElem.GetProperty("embeddingProvider").GetString(),
                    embeddingApiUrl = setElem.GetProperty("embeddingApiUrl").GetString(),
                    embeddingApiKey = setElem.GetProperty("embeddingApiKey").GetString(),
                    embeddingApiModel = setElem.GetProperty("embeddingApiModel").GetString(),
                    globalMaxKeys = setElem.GetProperty("globalMaxKeys").GetInt32(),
                    userMaxKeys = setElem.GetProperty("userMaxKeys").GetInt32()
                });

                // 5. Create Group Mapping (Domain Admins -> full_admin)
                await SendToolCallAsync("manage_group_mappings", new
                {
                    action = "save",
                    externalId = "S-1-5-32-544",
                    internalGroup = "full_admin"
                });

                // 6. Create Target Access Policy
                await SendToolCallAsync("manage_policies", new
                {
                    action = "save",
                    targetId = "docker",
                    requiredGroup = "devops",
                    isAllowed = true
                });

                // 7. Register Backend MCP Server
                var serverRaw = File.ReadAllText(Path.Combine(templatesDir, "server-docker-mcp.json"));
                using var serverDoc = JsonDocument.Parse(serverRaw);
                var srvElem = serverDoc.RootElement;
                await SendToolCallAsync("manage_servers", new
                {
                    action = "create",
                    id = srvElem.GetProperty("id").GetString(),
                    displayName = srvElem.GetProperty("displayName").GetString(),
                    url = srvElem.GetProperty("url").GetString(),
                    type = srvElem.GetProperty("type").GetString(),
                    enabled = srvElem.GetProperty("enabled").GetBoolean(),
                    hidden = srvElem.GetProperty("hidden").GetBoolean(),
                    secretProvider = srvElem.GetProperty("secretProvider").GetString(),
                    authShape = srvElem.GetProperty("authShape").GetString(),
                    categories = new[] { "infrastructure", "devops" }
                });

                // 8. Issue Developer AppKey
                var keyDoc = await SendToolCallAsync("manage_appkeys", new
                {
                    action = "create",
                    name = "Dev Automation Key",
                    username = "devops-engineer",
                    scopes = new[] { "all" },
                    expiresInDays = 90
                });
                Assert.Contains("plaintextKey", keyDoc.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);

                // 9. Verify All Provisioned Entities via List Queries
                var listProvidersDoc = await SendToolCallAsync("manage_providers", new { action = "list", type = "all" });
                Assert.Contains("HeaderAuth", listProvidersDoc.RootElement.GetRawText());
                Assert.Contains("HashiCorpVault", listProvidersDoc.RootElement.GetRawText());

                var listServersDoc = await SendToolCallAsync("manage_servers", new { action = "list" });
                Assert.Contains("docker", listServersDoc.RootElement.GetRawText());

                var getSettingsDoc = await SendToolCallAsync("manage_settings", new { action = "get" });
                Assert.Contains("OpenAI", getSettingsDoc.RootElement.GetRawText());

                var listMappingsDoc = await SendToolCallAsync("manage_group_mappings", new { action = "list" });
                Assert.Contains("S-1-5-32-544", listMappingsDoc.RootElement.GetRawText());

                var listPoliciesDoc = await SendToolCallAsync("manage_policies", new { action = "list" });
                Assert.Contains("docker", listPoliciesDoc.RootElement.GetRawText());
            }
            finally
            {
                if (File.Exists(tempDbFile))
                {
                    try { File.Delete(tempDbFile); } catch { }
                }
            }
        }

        [Fact]
        [Requirement("SEC-GATEWAY-ZERO-CONFIG-BOOT", "SEC", RequirementType.Positive, "Gateway boots from a blank slate with zero master key environment variables, auto-generates .master.key, and serves health and admin endpoints.")]
        public async Task Gateway_BlankSlate_WithoutMasterKeyEnv_AutoGeneratesKeyFileAndBootsSuccessfully()
        {
            ModelContextGateway.Infrastructure.Secrets.DbKeyHelper.ResetCache();
            ModelContextGateway.Infrastructure.Secrets.EncryptionKeyProvider.ResetCache();

            var tempDir = Path.Combine(Path.GetTempPath(), $"mcp_zero_config_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var tempDbFile = Path.Combine(tempDir, "router.db");

            try
            {
                using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { "DATA_DIR", tempDir },
                            { "ConnectionStrings:Sqlite", $"Data Source={tempDbFile}" },
                            { "Admin:StandaloneAllowedNetworks:0", "127.0.0.1" },
                            { "Admin:StandaloneAllowedNetworks:1", "::1" }
                        });
                    });
                });

                var client = factory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

                // 1. Health Probe
                var healthResp = await client.GetAsync("/health");
                Assert.Equal(System.Net.HttpStatusCode.OK, healthResp.StatusCode);

                // 2. Assert .master.key was auto-generated
                var keyFilePath = Path.Combine(tempDir, ".master.key");
                Assert.True(File.Exists(keyFilePath), "Persistent .master.key file must be auto-generated in data dir.");
                var generatedKey = File.ReadAllText(keyFilePath).Trim();
                Assert.False(string.IsNullOrWhiteSpace(generatedKey));
                Assert.Equal(32, Convert.FromBase64String(generatedKey).Length);

                // 3. Admin MCP Server invocation with seeded key
                client.DefaultRequestHeaders.Add("Authorization", "Bearer mcp-global-admin-default-cli-key-99");
                var payload = new
                {
                    jsonrpc = "2.0",
                    id = "zero-config-test",
                    method = "tools/call",
                    @params = new
                    {
                        name = "manage_system",
                        arguments = new { action = "diagnostics" }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var adminResp = await client.PostAsync("/admin", content);
                Assert.Equal(System.Net.HttpStatusCode.OK, adminResp.StatusCode);

                var adminBody = await adminResp.Content.ReadAsStringAsync();
                Assert.Contains("activeSessions", adminBody);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}

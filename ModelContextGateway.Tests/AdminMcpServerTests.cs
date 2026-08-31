using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ModelContextGateway.Tests
{
    public class AdminMcpServerTests : IDisposable
    {
        private class NonDisposingConnection : IDbConnection
        {
            private readonly IDbConnection _inner;
            public NonDisposingConnection(IDbConnection inner) => _inner = inner;
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public string ConnectionString { get => _inner.ConnectionString ?? string.Empty; set => _inner.ConnectionString = value ?? string.Empty; }
            public int ConnectionTimeout => _inner.ConnectionTimeout;
            public string Database => _inner.Database;
            public ConnectionState State => _inner.State;
            public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
            public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
            public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
            public void Close() { }
            public IDbCommand CreateCommand() => _inner.CreateCommand();
            public void Dispose() { }
            public void Open()
            {
                if (_inner.State != ConnectionState.Open)
                {
                    _inner.Open();
                }
            }
        }

        private readonly SqliteConnection _rawConnection;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _config;
        private readonly Mock<IAuditLogger> _mockAuditLogger;
        private readonly DatabaseRepository _dbRepo;
        private readonly ICredentialService _credentialService;
        private readonly SessionManager _sessionManager;
        private readonly DynamicEmbeddingService _dynamicEmbeddingService;
        private readonly BackendHealthCheckService _healthCheckService;
        private readonly AdminMcpServer _adminMcpServer;

        public AdminMcpServerTests()
        {
            _rawConnection = new SqliteConnection($"DataSource=file:mem_admin_{Guid.NewGuid():N}?mode=memory&cache=shared");
            _rawConnection.Open();

            _rawConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS Servers (
                    Id TEXT PRIMARY KEY, DisplayName TEXT, Url TEXT, Enabled INTEGER DEFAULT 1, Hidden INTEGER DEFAULT 0,
                    Type TEXT DEFAULT 'sse', SecretProvider TEXT, SecretItemKey TEXT, SecretMount TEXT, SecretPath TEXT,
                    SecretField TEXT, AuthShape TEXT, CustomHeaderName TEXT, Categories TEXT DEFAULT '[]', ApiKey TEXT,
                    HeadersJson TEXT, AutoDiscovered INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS AppKeys (
                    Id TEXT PRIMARY KEY, Name TEXT, Username TEXT, OwnerSid TEXT DEFAULT '', KeyType TEXT DEFAULT 'personal', KeyPrefix TEXT,
                    EncryptedKey TEXT, ScopesJson TEXT DEFAULT '[]', ExpiresAt TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Id TEXT PRIMARY KEY, DashboardTitle TEXT DEFAULT 'MCP Gateway', DashboardIcon TEXT DEFAULT 'fa-solid fa-network-wired',
                    EmbeddingProvider TEXT DEFAULT 'onnx', EmbeddingApiUrl TEXT, EmbeddingApiKey TEXT, EmbeddingApiModel TEXT,
                    EmbeddingModelDir TEXT, GlobalMaxKeys INTEGER DEFAULT 100, UserMaxKeys INTEGER DEFAULT 5
                );
                CREATE TABLE IF NOT EXISTS AccessPolicies (
                    Id TEXT PRIMARY KEY, TargetId TEXT, RequiredGroup TEXT, IsAllowed INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS GroupMappings (
                    Id TEXT PRIMARY KEY, ExternalId TEXT, InternalGroup TEXT
                );
                CREATE TABLE IF NOT EXISTS SecretProviders (
                    ProviderName TEXT PRIMARY KEY, DisplayName TEXT, EncryptedConfigJson TEXT, IsEnabled INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS AuthProviderConfigs (
                    ProviderName TEXT PRIMARY KEY, DisplayName TEXT, UserHeader TEXT, GroupsHeader TEXT, EncryptedConfigJson TEXT, IsEnabled INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    RequestId TEXT, UserPrincipalName TEXT, UserSid TEXT, ServerCodeName TEXT, ItemName TEXT,
                    RequestMethod TEXT, ExecutionTimeMs INTEGER, StatusCode INTEGER, RequestPayload TEXT, ResponsePayload TEXT,
                    ErrorMessage TEXT, Timestamp TEXT DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS AdminAuditLogs (
                    Id TEXT PRIMARY KEY, Username TEXT, Action TEXT, Target TEXT, Details TEXT, Success INTEGER,
                    ErrorMessage TEXT, Timestamp TEXT DEFAULT CURRENT_TIMESTAMP
                );");

            _rawConnection.Execute("INSERT OR REPLACE INTO Settings (Id, GlobalMaxKeys, UserMaxKeys) VALUES ('default', 100, 5);");

            var mockFactory = new Mock<IDbConnectionFactory>();
            mockFactory.Setup(f => f.CreateConnection()).Returns(() => new NonDisposingConnection(_rawConnection));
            mockFactory.Setup(f => f.ProviderName).Returns("sqlite");
            _dbFactory = mockFactory.Object;

            _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DB_ENCRYPTION_KEY", "TestSecretKey1234567890123456789012" },
                { "Admin:GroupSid", "full_admin" },
                { "Security:AllowedIpRanges:0", "127.0.0.0/8" },
                { "Security:AllowedIpRanges:1", "::1" }
            }).Build();

            _mockAuditLogger = new Mock<IAuditLogger>();
            _dbRepo = new DatabaseRepository(_dbFactory, _config);
            _credentialService = new CredentialService(_dbFactory);

            var services = new ServiceCollection();
            services.AddSingleton(_config);
            services.AddSingleton(_dbFactory);
            services.AddLogging();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            _dynamicEmbeddingService = new DynamicEmbeddingService(httpClientFactory.CreateClient(), serviceProvider.GetRequiredService<ILoggerFactory>(), serviceProvider);
            _sessionManager = new SessionManager(serviceProvider, httpClientFactory, serviceProvider.GetRequiredService<ILogger<SessionManager>>());
            _healthCheckService = new BackendHealthCheckService(serviceProvider, httpClientFactory, _sessionManager, serviceProvider.GetRequiredService<ILogger<BackendHealthCheckService>>());

            _adminMcpServer = new AdminMcpServer(
                _dbRepo,
                _dbRepo,
                _dbRepo,
                _dbRepo,
                _dbRepo,
                _dbFactory,
                _mockAuditLogger.Object,
                _credentialService,
                _healthCheckService,
                _dynamicEmbeddingService,
                _sessionManager,
                ldapService: null,
                httpClient: httpClientFactory.CreateClient(),
                configuration: _config,
                logger: serviceProvider.GetRequiredService<ILogger<AdminMcpServer>>()
            );
        }

        public void Dispose()
        {
            _rawConnection.Dispose();
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOLS-LIST-COUNT", "MCP", RequirementType.Positive, "AdminMcpServer tools/list returns all 10 consolidated tools with complete JSON schemas.")]
        public async Task ListToolsAsync_ReturnsTenConsolidatedTools()
        {
            var tools = await _adminMcpServer.ListToolsAsync();

            Assert.NotNull(tools);
            Assert.Equal(10, tools.Count);

            var expectedToolNames = new HashSet<string>
            {
                "manage_servers",
                "manage_appkeys",
                "manage_clients",
                "manage_policies",
                "manage_group_mappings",
                "manage_providers",
                "manage_settings",
                "manage_custom_files",
                "manage_system",
                "test_tool_call"
            };

            var json = JsonSerializer.Serialize(tools);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            var foundNames = new HashSet<string>();

            foreach (var toolElem in root.EnumerateArray())
            {
                Assert.True(toolElem.TryGetProperty("name", out var nameProp));
                var name = nameProp.GetString()!;
                foundNames.Add(name);

                Assert.True(toolElem.TryGetProperty("description", out var descProp));
                Assert.False(string.IsNullOrWhiteSpace(descProp.GetString()));

                Assert.True(toolElem.TryGetProperty("inputSchema", out var schemaProp));
                Assert.True(schemaProp.TryGetProperty("type", out var typeProp));
                Assert.Equal("object", typeProp.GetString());
                Assert.True(schemaProp.TryGetProperty("properties", out _));
            }

            Assert.Equal(expectedToolNames, foundNames);
        }

        [Fact]
        [Requirement("MCP-ADMIN-DISCOVER-HANDSHAKE", "MCP", RequirementType.Positive, "AdminMcpServer server/discover RPC advertises supported protocol versions, capabilities, and identity.")]
        public async Task ProcessRequestAsync_ServerDiscover_ReturnsCapabilitiesAndVersions()
        {
            var request = new JsonRpcRequest
            {
                Id = 123,
                Method = "server/discover"
            };

            var response = await _adminMcpServer.ProcessRequestAsync(request);
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.NotNull(response.Result);

            var json = JsonSerializer.Serialize(response.Result);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("supportedVersions", out var versionsProp));
            Assert.Equal(JsonValueKind.Array, versionsProp.ValueKind);
            var versions = versionsProp.EnumerateArray().Select(v => v.GetString()).ToList();
            Assert.Contains("2026-07-28", versions);
            Assert.Contains("2024-11-05", versions);

            Assert.True(root.TryGetProperty("capabilities", out var capsProp));
            Assert.True(capsProp.TryGetProperty("tools", out _));

            Assert.True(root.TryGetProperty("serverInfo", out var infoProp));
            Assert.Equal("Model-Context-Gateway-Admin", infoProp.GetProperty("name").GetString());
        }

        [Fact]
        [Requirement("MCP-ADMIN-INITIALIZE-HANDSHAKE", "MCP", RequirementType.Positive, "AdminMcpServer initialize handles protocol negotiation for 2026-07-28 and 2024-11-05.")]
        public async Task HandleInitializeAsync_NegotiatesProtocolVersion()
        {
            // Default initialization
            var initResultDefault = await _adminMcpServer.HandleInitializeAsync(null);
            var jsonDefault = JsonSerializer.Serialize(initResultDefault);
            using var docDefault = JsonDocument.Parse(jsonDefault);
            Assert.Equal("2026-07-28", docDefault.RootElement.GetProperty("protocolVersion").GetString());
            Assert.Equal("Model-Context-Gateway-Admin", docDefault.RootElement.GetProperty("serverInfo").GetProperty("name").GetString());
            Assert.True(docDefault.RootElement.GetProperty("capabilities").TryGetProperty("extensions", out _));

            // Backward compatibility negotiation to 2024-11-05
            var params2024 = JsonDocument.Parse("{\"protocolVersion\":\"2024-11-05\"}").RootElement;
            var initResult2024 = await _adminMcpServer.HandleInitializeAsync(params2024);
            var json2024 = JsonSerializer.Serialize(initResult2024);
            using var doc2024 = JsonDocument.Parse(json2024);
            Assert.Equal("2024-11-05", doc2024.RootElement.GetProperty("protocolVersion").GetString());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-SERVERS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_servers list, get, create, update, toggle, and delete actions.")]
        public async Task CallToolAsync_ManageServers_ListAndCreate()
        {
            // 1. Create server
            var createArgs = JsonDocument.Parse(@"{
                ""action"": ""create"",
                ""id"": ""srv-test"",
                ""displayName"": ""Test Server"",
                ""url"": ""http://localhost:9000/sse"",
                ""type"": ""sse"",
                ""enabled"": true
            }").RootElement;

            var createRes = await _adminMcpServer.CallToolAsync("manage_servers", createArgs, "admin_user");
            Assert.NotNull(createRes);

            var createJson = JsonSerializer.Serialize(createRes);
            using var createDoc = JsonDocument.Parse(createJson);
            Assert.False(createDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. Get server
            var getArgs = JsonDocument.Parse(@"{ ""action"": ""get"", ""id"": ""srv-test"" }").RootElement;
            var getRes = await _adminMcpServer.CallToolAsync("manage_servers", getArgs, "admin_user");
            var getJson = JsonSerializer.Serialize(getRes);
            using var getDoc = JsonDocument.Parse(getJson);
            Assert.False(getDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("Test Server", getDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. List servers
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_servers", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("srv-test", listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 4. Toggle server
            var toggleArgs = JsonDocument.Parse(@"{ ""action"": ""toggle"", ""id"": ""srv-test"", ""enabled"": false }").RootElement;
            var toggleRes = await _adminMcpServer.CallToolAsync("manage_servers", toggleArgs, "admin_user");
            var toggleJson = JsonSerializer.Serialize(toggleRes);
            using var toggleDoc = JsonDocument.Parse(toggleJson);
            Assert.False(toggleDoc.RootElement.GetProperty("isError").GetBoolean());

            // 5. Delete server
            var deleteArgs = JsonDocument.Parse(@"{ ""action"": ""delete"", ""id"": ""srv-test"" }").RootElement;
            var deleteRes = await _adminMcpServer.CallToolAsync("manage_servers", deleteArgs, "admin_user");
            var deleteJson = JsonSerializer.Serialize(deleteRes);
            using var deleteDoc = JsonDocument.Parse(deleteJson);
            Assert.False(deleteDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-AUDIT-LOG", "MCP", RequirementType.Positive, "AdminMcpServer tool calls record audit log entries with caller and tool name.")]
        public async Task CallToolAsync_RecordsAuditLog()
        {
            var args = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            await _adminMcpServer.CallToolAsync("manage_servers", args, "steve");

            _mockAuditLogger.Verify(a => a.LogAdminActionAsync(
                "steve",
                It.Is<string>(act => act.Contains("manage_servers") || act.Contains("servers")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                null
            ), Times.AtLeastOnce);
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-APPKEYS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_appkeys create, list, limits, and revoke actions.")]
        public async Task CallToolAsync_ManageAppKeys_Lifecycle()
        {
            // 1. Create AppKey
            var createArgs = JsonDocument.Parse(@"{
                ""action"": ""create"",
                ""name"": ""Admin Test Key"",
                ""username"": ""alice"",
                ""scopes"": [""all""],
                ""expiresInDays"": 30
            }").RootElement;

            var createRes = await _adminMcpServer.CallToolAsync("manage_appkeys", createArgs, "admin_user");
            var createJson = JsonSerializer.Serialize(createRes);
            using var createDoc = JsonDocument.Parse(createJson);
            Assert.False(createDoc.RootElement.GetProperty("isError").GetBoolean());
            var text = createDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()!;
            Assert.Contains("plaintextKey", text, StringComparison.OrdinalIgnoreCase);

            using var payloadDoc = JsonDocument.Parse(text);
            var keyId = payloadDoc.RootElement.GetProperty("id").GetString()!;

            // 2. Get Limits
            var limitsArgs = JsonDocument.Parse(@"{ ""action"": ""get_limits"", ""username"": ""alice"" }").RootElement;
            var limitsRes = await _adminMcpServer.CallToolAsync("manage_appkeys", limitsArgs, "admin_user");
            var limitsJson = JsonSerializer.Serialize(limitsRes);
            using var limitsDoc = JsonDocument.Parse(limitsJson);
            Assert.False(limitsDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("globalMax", limitsDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. List Keys
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"", ""username"": ""alice"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_appkeys", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains(keyId, listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 4. Revoke Key
            var revokeArgs = JsonDocument.Parse($@"{{ ""action"": ""revoke"", ""id"": ""{keyId}"" }}").RootElement;
            var revokeRes = await _adminMcpServer.CallToolAsync("manage_appkeys", revokeArgs, "admin_user");
            var revokeJson = JsonSerializer.Serialize(revokeRes);
            using var revokeDoc = JsonDocument.Parse(revokeJson);
            Assert.False(revokeDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-CLIENTS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_clients register, list, and delete actions.")]
        public async Task CallToolAsync_ManageClients_Lifecycle()
        {
            // 1. Register client
            var regArgs = JsonDocument.Parse(@"{
                ""action"": ""register"",
                ""displayName"": ""Agent Client"",
                ""scopes"": [""category:database""],
                ""expiresInDays"": 60
            }").RootElement;

            var regRes = await _adminMcpServer.CallToolAsync("manage_clients", regArgs, "admin_user");
            var regJson = JsonSerializer.Serialize(regRes);
            using var regDoc = JsonDocument.Parse(regJson);
            Assert.False(regDoc.RootElement.GetProperty("isError").GetBoolean());
            var text = regDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()!;
            Assert.Contains("clientId", text, StringComparison.OrdinalIgnoreCase);

            using var payloadDoc = JsonDocument.Parse(text);
            var clientId = payloadDoc.RootElement.GetProperty("clientId").GetString()!;

            // 2. List clients
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_clients", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("Agent Client", listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. Delete client
            var delArgs = JsonDocument.Parse($@"{{ ""action"": ""delete"", ""id"": ""{clientId}"" }}").RootElement;
            var delRes = await _adminMcpServer.CallToolAsync("manage_clients", delArgs, "admin_user");
            var delJson = JsonSerializer.Serialize(delRes);
            using var delDoc = JsonDocument.Parse(delJson);
            Assert.False(delDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-POLICIES", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_policies save, list, and delete actions.")]
        public async Task CallToolAsync_ManagePolicies_Lifecycle()
        {
            // 1. Save policy
            var saveArgs = JsonDocument.Parse(@"{
                ""action"": ""save"",
                ""id"": ""pol-1"",
                ""targetId"": ""docker"",
                ""requiredGroup"": ""devops"",
                ""isAllowed"": true
            }").RootElement;

            var saveRes = await _adminMcpServer.CallToolAsync("manage_policies", saveArgs, "admin_user");
            var saveJson = JsonSerializer.Serialize(saveRes);
            using var saveDoc = JsonDocument.Parse(saveJson);
            Assert.False(saveDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. List policies
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_policies", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("devops", listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. Delete policy
            var delArgs = JsonDocument.Parse(@"{ ""action"": ""delete"", ""id"": ""pol-1"" }").RootElement;
            var delRes = await _adminMcpServer.CallToolAsync("manage_policies", delArgs, "admin_user");
            var delJson = JsonSerializer.Serialize(delRes);
            using var delDoc = JsonDocument.Parse(delJson);
            Assert.False(delDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-GROUP-MAPPINGS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_group_mappings save, list, and delete actions.")]
        public async Task CallToolAsync_ManageGroupMappings_Lifecycle()
        {
            // 1. Save mapping
            var saveArgs = JsonDocument.Parse(@"{
                ""action"": ""save"",
                ""id"": ""map-1"",
                ""externalId"": ""S-1-5-21-12345"",
                ""internalGroup"": ""full_admin""
            }").RootElement;

            var saveRes = await _adminMcpServer.CallToolAsync("manage_group_mappings", saveArgs, "admin_user");
            var saveJson = JsonSerializer.Serialize(saveRes);
            using var saveDoc = JsonDocument.Parse(saveJson);
            Assert.False(saveDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. List mappings
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_group_mappings", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("S-1-5-21-12345", listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. Delete mapping
            var delArgs = JsonDocument.Parse(@"{ ""action"": ""delete"", ""id"": ""map-1"" }").RootElement;
            var delRes = await _adminMcpServer.CallToolAsync("manage_group_mappings", delArgs, "admin_user");
            var delJson = JsonSerializer.Serialize(delRes);
            using var delDoc = JsonDocument.Parse(delJson);
            Assert.False(delDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-PROVIDERS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_providers list, save_secret, and save_auth actions.")]
        public async Task CallToolAsync_ManageProviders_Lifecycle()
        {
            // 1. Save secret provider
            var saveSecretArgs = JsonDocument.Parse(@"{
                ""action"": ""save_secret"",
                ""providerName"": ""vault"",
                ""displayName"": ""HashiCorp Vault"",
                ""configJson"": ""{\""Address\"":\""https://vault.local:8200\"",\""Token\"":\""s.123456\""}"",
                ""isEnabled"": true
            }").RootElement;

            var saveSecRes = await _adminMcpServer.CallToolAsync("manage_providers", saveSecretArgs, "admin_user");
            var saveSecJson = JsonSerializer.Serialize(saveSecRes);
            using var saveSecDoc = JsonDocument.Parse(saveSecJson);
            Assert.False(saveSecDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. Save auth provider
            var saveAuthArgs = JsonDocument.Parse(@"{
                ""action"": ""save_auth"",
                ""providerName"": ""ldap"",
                ""displayName"": ""Active Directory"",
                ""userHeader"": ""Remote-User"",
                ""groupsHeader"": ""Remote-Groups"",
                ""configJson"": ""{\""Server\"":\""dc1.corp.internal\"",\""Port\"":636}"",
                ""isEnabled"": true
            }").RootElement;

            var saveAuthRes = await _adminMcpServer.CallToolAsync("manage_providers", saveAuthArgs, "admin_user");
            var saveAuthJson = JsonSerializer.Serialize(saveAuthRes);
            using var saveAuthDoc = JsonDocument.Parse(saveAuthJson);
            Assert.False(saveAuthDoc.RootElement.GetProperty("isError").GetBoolean());

            // 3. List providers
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_providers", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("vault", listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-SETTINGS", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_settings get and update actions.")]
        public async Task CallToolAsync_ManageSettings_Lifecycle()
        {
            // 1. Get settings
            var getArgs = JsonDocument.Parse(@"{ ""action"": ""get"" }").RootElement;
            var getRes = await _adminMcpServer.CallToolAsync("manage_settings", getArgs, "admin_user");
            var getJson = JsonSerializer.Serialize(getRes);
            using var getDoc = JsonDocument.Parse(getJson);
            Assert.False(getDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. Update settings
            var updateArgs = JsonDocument.Parse(@"{
                ""action"": ""update"",
                ""dashboardTitle"": ""Custom MCG Hub"",
                ""globalMaxKeys"": 200,
                ""userMaxKeys"": 10
            }").RootElement;

            var updateRes = await _adminMcpServer.CallToolAsync("manage_settings", updateArgs, "admin_user");
            var updateJson = JsonSerializer.Serialize(updateRes);
            using var updateDoc = JsonDocument.Parse(updateJson);
            Assert.False(updateDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("Custom MCG Hub", updateDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-CUSTOM-FILES", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_custom_files save, get, list, and delete actions.")]
        public async Task CallToolAsync_ManageCustomFiles_Lifecycle()
        {
            var fileName = $"test-prompt-{Guid.NewGuid():N}.json";
            var promptJson = "{\"name\":\"test_prompt\",\"description\":\"A test prompt\"}";

            // 1. Save custom file
            var saveArgs = JsonDocument.Parse($@"{{
                ""action"": ""save"",
                ""type"": ""prompts"",
                ""name"": ""{fileName}"",
                ""content"": {JsonSerializer.Serialize(promptJson)}
            }}").RootElement;

            var saveRes = await _adminMcpServer.CallToolAsync("manage_custom_files", saveArgs, "admin_user");
            var saveJson = JsonSerializer.Serialize(saveRes);
            using var saveDoc = JsonDocument.Parse(saveJson);
            Assert.False(saveDoc.RootElement.GetProperty("isError").GetBoolean());

            // 2. Get custom file
            var getArgs = JsonDocument.Parse($@"{{
                ""action"": ""get"",
                ""type"": ""prompts"",
                ""name"": ""{fileName}""
            }}").RootElement;

            var getRes = await _adminMcpServer.CallToolAsync("manage_custom_files", getArgs, "admin_user");
            var getJson = JsonSerializer.Serialize(getRes);
            using var getDoc = JsonDocument.Parse(getJson);
            Assert.False(getDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("test_prompt", getDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 3. List custom files
            var listArgs = JsonDocument.Parse(@"{ ""action"": ""list"", ""type"": ""prompts"" }").RootElement;
            var listRes = await _adminMcpServer.CallToolAsync("manage_custom_files", listArgs, "admin_user");
            var listJson = JsonSerializer.Serialize(listRes);
            using var listDoc = JsonDocument.Parse(listJson);
            Assert.False(listDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains(fileName, listDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 4. Delete custom file
            var delArgs = JsonDocument.Parse($@"{{
                ""action"": ""delete"",
                ""type"": ""prompts"",
                ""name"": ""{fileName}""
            }}").RootElement;

            var delRes = await _adminMcpServer.CallToolAsync("manage_custom_files", delArgs, "admin_user");
            var delJson = JsonSerializer.Serialize(delRes);
            using var delDoc = JsonDocument.Parse(delJson);
            Assert.False(delDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-MANAGE-SYSTEM", "MCP", RequirementType.Positive, "AdminMcpServer executes manage_system diagnostics, get_logs, clear_logs, and query_audit actions.")]
        public async Task CallToolAsync_ManageSystem_Lifecycle()
        {
            // 1. Diagnostics
            var diagArgs = JsonDocument.Parse(@"{ ""action"": ""diagnostics"" }").RootElement;
            var diagRes = await _adminMcpServer.CallToolAsync("manage_system", diagArgs, "admin_user");
            var diagJson = JsonSerializer.Serialize(diagRes);
            using var diagDoc = JsonDocument.Parse(diagJson);
            Assert.False(diagDoc.RootElement.GetProperty("isError").GetBoolean());
            Assert.Contains("activeSessions", diagDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString());

            // 2. Get Logs
            var logsArgs = JsonDocument.Parse(@"{ ""action"": ""get_logs"", ""limit"": 10 }").RootElement;
            var logsRes = await _adminMcpServer.CallToolAsync("manage_system", logsArgs, "admin_user");
            var logsJson = JsonSerializer.Serialize(logsRes);
            using var logsDoc = JsonDocument.Parse(logsJson);
            Assert.False(logsDoc.RootElement.GetProperty("isError").GetBoolean());

            // 3. Clear Logs
            var clearArgs = JsonDocument.Parse(@"{ ""action"": ""clear_logs"" }").RootElement;
            var clearRes = await _adminMcpServer.CallToolAsync("manage_system", clearArgs, "admin_user");
            var clearJson = JsonSerializer.Serialize(clearRes);
            using var clearDoc = JsonDocument.Parse(clearJson);
            Assert.False(clearDoc.RootElement.GetProperty("isError").GetBoolean());

            // 4. Query Audit
            var auditArgs = JsonDocument.Parse(@"{ ""action"": ""query_audit"", ""take"": 10 }").RootElement;
            var auditRes = await _adminMcpServer.CallToolAsync("manage_system", auditArgs, "admin_user");
            var auditJson = JsonSerializer.Serialize(auditRes);
            using var auditDoc = JsonDocument.Parse(auditJson);
            Assert.False(auditDoc.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("GUARD-ADMIN-UNKNOWN-TOOL", "GUARD", RequirementType.Negative, "AdminMcpServer returns an error response for unknown tool or action invocations.")]
        public async Task CallToolAsync_UnknownToolOrAction_ReturnsErrorResponse()
        {
            var unknownToolArgs = JsonDocument.Parse(@"{ ""action"": ""test"" }").RootElement;
            var resUnknown = await _adminMcpServer.CallToolAsync("non_existent_tool", unknownToolArgs, "admin_user");
            var jsonUnknown = JsonSerializer.Serialize(resUnknown);
            using var docUnknown = JsonDocument.Parse(jsonUnknown);
            Assert.True(docUnknown.RootElement.GetProperty("isError").GetBoolean());

            var invalidActionArgs = JsonDocument.Parse(@"{ ""action"": ""invalid_action_name"" }").RootElement;
            var resInvalid = await _adminMcpServer.CallToolAsync("manage_servers", invalidActionArgs, "admin_user");
            var jsonInvalid = JsonSerializer.Serialize(resInvalid);
            using var docInvalid = JsonDocument.Parse(jsonInvalid);
            Assert.True(docInvalid.RootElement.GetProperty("isError").GetBoolean());
        }

        [Fact]
        [Requirement("SEC-ADMIN-AUDIT-REDACTION", "SEC", RequirementType.Positive, "AdminMcpServer redacts sensitive secrets from argument payloads before recording audit logs.")]
        public async Task CallToolAsync_AuditLog_RedactsSensitivePayloadData()
        {
            var saveSecretArgs = JsonDocument.Parse(@"{
                ""action"": ""save_secret"",
                ""providerName"": ""vault"",
                ""displayName"": ""HashiCorp Vault"",
                ""configJson"": ""{\""Address\"":\""https://vault.local:8200\"",\""Token\"":\""super-secret-token-xyz\""}"",
                ""isEnabled"": true
            }").RootElement;

            await _adminMcpServer.CallToolAsync("manage_providers", saveSecretArgs, "admin_auditor");

            _mockAuditLogger.Verify(a => a.LogAdminActionAsync(
                "admin_auditor",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(details => !details.Contains("super-secret-token-xyz") && (details.Contains("********") || !details.Contains("Token"))),
                true,
                null
            ), Times.AtLeastOnce);
        }

        [Fact]
        [Requirement("MCP-ADMIN-TOOL-TEST-CALL-ERROR", "GUARD", RequirementType.Negative, "AdminMcpServer test_tool_call propagates downstream backend errors with visibility.")]
        public async Task CallToolAsync_TestToolCall_MissingServer_ReturnsError()
        {
            var testArgs = JsonDocument.Parse(@"{
                ""serverId"": ""non-existent-server"",
                ""toolName"": ""some_tool""
            }").RootElement;

            var res = await _adminMcpServer.CallToolAsync("test_tool_call", testArgs, "admin_user");
            var json = JsonSerializer.Serialize(res);
            using var doc = JsonDocument.Parse(json);

            Assert.True(doc.RootElement.GetProperty("isError").GetBoolean());
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()!;
            Assert.Contains("non-existent-server", text);
        }
    }
}

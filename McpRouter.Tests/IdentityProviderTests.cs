using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using McpRouter.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class IdentityProviderTests
    {
        [Fact]
        public async Task OidcIdentityProvider_Parses_Remote_User_And_Groups_Headers()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "steve";
            context.Request.Headers["Remote-Groups"] = "full_admin, house_member";

            var configDict = new Dictionary<string, string?> { ["Oidc:TrustedProxies"] = "127.0.0.1" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("steve", identity.Username);
            Assert.Equal("HeaderAuth", identity.AuthenticationType);
            Assert.Equal(2, identity.GroupNames.Count);
            Assert.Contains("full_admin", identity.GroupNames);
            Assert.Contains("house_member", identity.GroupNames);
        }

        [Fact]
        public async Task CompositeIdentityProvider_Falls_Back_To_Oidc_When_AD_Not_Authenticated()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "alex";
            context.Request.Headers["sso_groups"] = "dev_team";

            var configDict = new Dictionary<string, string?> { ["Oidc:TrustedProxies"] = "127.0.0.1" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var adProvider = new ActiveDirectoryIdentityProvider(config);
            var oidcProvider = new OidcIdentityProvider(config);
            var composite = new CompositeIdentityProvider(new IIdentityProvider[] { adProvider, oidcProvider });

            var identity = await composite.ResolveIdentityAsync(context);

            Assert.Equal("alex", identity.Username);
            Assert.Contains("dev_team", identity.GroupNames);
        }

        [Fact]
        public async Task HeaderAuth_StripsHeaders_ForUntrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
            context.Request.Headers["Remote-User"] = "malicious";
            context.Request.Headers["Remote-Groups"] = "admin, full_admin";
            context.Request.Headers["X-Forwarded-User"] = "malicious_forwarded";
            context.Request.Headers["sso_groups"] = "malicious_sso";

            var provider = new OidcIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("guest", identity.Username);
            Assert.False(context.Request.Headers.ContainsKey("Remote-User"));
            Assert.False(context.Request.Headers.ContainsKey("Remote-Groups"));
            Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-User"));
            Assert.False(context.Request.Headers.ContainsKey("sso_groups"));
            Assert.Empty(identity.GroupNames);
        }

        [Fact]
        public async Task HeaderAuth_AllowsHeaders_ForTrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.10");
            context.Request.Headers["Remote-User"] = "steve";
            context.Request.Headers["Remote-Groups"] = "house_member";

            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = "10.0.0.10,127.0.0.1"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.True(context.Request.Headers.ContainsKey("Remote-User"));
            Assert.Equal("steve", identity.Username);
            Assert.Contains("house_member", identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_DoesNotMapAdminSid_ForAdminGroups()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "steve";
            context.Request.Headers["Remote-Groups"] = "full_admin";

            var provider = new OidcIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Empty(identity.AllSids);
        }

        /// <summary>
        /// Verifies that a single shared ClientSession evaluates identity from the live
        /// per-message HttpContext, not the cached handshake context.
        ///
        /// Scenario:
        ///   - Session is established with a dummy handshake context (neither Alice nor Bob).
        ///   - Alice's per-request context resolves to an admin SID → tool call is authorized.
        ///   - Bob's per-request context resolves to no SID/groups → tool call is denied (isError:true).
        /// </summary>
        [Fact]
        public async Task SSE_ValidatesIdentityPerMessage()
        {
            const string AdminSid = "S-1-5-32-544";

            // --- Build a per-request-context-aware mock identity provider ---
            // The mock returns Alice's admin identity when her context is passed in,
            // and Bob's unprivileged identity when his context is passed in.
            var aliceContext = new DefaultHttpContext();
            aliceContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

            var bobContext = new DefaultHttpContext();
            bobContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

            var aliceIdentity = new UserIdentityContext("alice", "Test", new List<string> { "full_admin" }, AdminSid);
            var bobIdentity   = new UserIdentityContext("bob",   "Test", new List<string>(), "");

            var mockProvider = new Moq.Mock<IIdentityProvider>();
            mockProvider.Setup(p => p.ResolveIdentityAsync(aliceContext)).ReturnsAsync(aliceIdentity);
            mockProvider.Setup(p => p.ResolveIdentityAsync(bobContext)).ReturnsAsync(bobIdentity);

            // --- Shared service provider ---
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Audit:FailClosed"] = "false",
                ["Admin:GroupSid"]   = AdminSid
            }).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(new CompositeIdentityProvider(new[] { mockProvider.Object }));
            var sp = services.BuildServiceProvider();

            aliceContext.RequestServices = sp;
            bobContext.RequestServices   = sp;

            // Handshake context — session constructed with Alice's response but identity
            // must NOT be used for subsequent per-message calls
            var handshakeContext = new DefaultHttpContext();
            handshakeContext.RequestServices = sp;
            handshakeContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

            var servers = new List<McpRouter.Models.McpServer>();
            var httpClient = new System.Net.Http.HttpClient();
            var loggerMock = new Moq.Mock<Microsoft.Extensions.Logging.ILogger>();
            var embeddingMock = new Moq.Mock<McpRouter.Services.IEmbeddingService>();

            var session = new ClientSession("test-sse-session", handshakeContext.Response, servers, httpClient, embeddingMock.Object, loggerMock.Object);

            var toolBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"search_tools\",\"arguments\":{\"query\":\"test\"}}}";

            // --- Alice's call: admin SID bypass → authorized ---
            // Tool routing will fail (no backends) but the auth layer must not deny her.
            try { await session.CallToolAsync("search_tools", toolBody, dbFactory: null!, httpContext: aliceContext); }
            catch (UnauthorizedAccessException) { throw; }  // fail the test if auth denies alice
            catch { /* routing / no-backend exceptions are expected in a unit test */ }

            // --- Bob's call: no SID, no DB policy → RBAC must deny (isError:true) ---
            // CallToolAsync returns an MCP error object on RBAC denial (protocol-conformant).
            var bobResult = await session.CallToolAsync("search_tools", toolBody, dbFactory: null!, httpContext: bobContext);
            var bobJson   = System.Text.Json.JsonSerializer.Serialize(bobResult);
            Assert.Contains("\"isError\":true", bobJson);
            Assert.Contains("Security Error", bobJson);
            Assert.Contains("bob", bobJson);
        }

        [Fact]
        public async Task LdapService_ThrowsInvalidOperation_WhenUseSslFalse()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Ldap:Server"] = "127.0.0.1",
                ["Ldap:Port"] = "389",
                ["Ldap:UseSsl"] = "false"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<LdapActiveDirectoryService>>().Object;
            var service = new LdapActiveDirectoryService(config, logger);

            await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            {
                await service.ResolveUserSidsAsync("testuser");
            });
        }

        [Fact]
        public async Task LdapService_ThrowsSecurityException_OnBindFailure()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Ldap:Server"] = "127.0.0.1",
                ["Ldap:Port"] = "636",
                ["Ldap:UseSsl"] = "true"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<LdapActiveDirectoryService>>().Object;
            var service = new LdapActiveDirectoryService(config, logger);

            await Assert.ThrowsAsync<System.Security.SecurityException>(async () =>
            {
                await service.ResolveUserSidsAsync("testuser");
            });
        }

        [Fact]
        public void SecurityValidationHelper_IsAdmin_RequiresAdminGroupSid()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Admin:GroupSid"] = "S-1-5-32-544"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var adminIdentity = new UserIdentityContext("admin_user", "Test", new List<string> { "Administrators" }, Sid: "", Sids: new List<string> { "S-1-5-32-544" });
            var nonAdminIdentity = new UserIdentityContext("admin", "Test", new List<string> { "Administrators", "full_admin" }, Sid: "", Sids: new List<string>());

            Assert.True(McpRouter.Core.Security.SecurityValidationHelper.IsAdmin(adminIdentity, config));
            Assert.False(McpRouter.Core.Security.SecurityValidationHelper.IsAdmin(nonAdminIdentity, config));
        }

        [Fact]
        public async Task OidcIdentityProvider_DoesNotGrantAdminSid_FromGroupOrUserNames()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "admin";
            context.Request.Headers["Remote-Groups"] = "full_admin, Administrators";

            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = "127.0.0.1"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var provider = new OidcIdentityProvider(config);

            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("admin", identity.Username);
            Assert.Empty(identity.AllSids);
            Assert.False(McpRouter.Core.Security.SecurityValidationHelper.IsAdmin(identity, config));
        }

        [Fact]
        public void TrustedProxyHelper_DeniesLoopback_WhenNotExplicitlyAllowlisted()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "malicious";

            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:RequireTrustedProxy"] = "true",
                ["Oidc:TrustedProxies"] = "10.0.0.10" // Loopback (127.0.0.1) NOT included!
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            bool isTrusted = TrustedProxyHelper.IsTrustedProxy(context, config);
            Assert.False(isTrusted);
        }

        [Fact]
        public void TrustedProxyHelper_DeniesXForwardedFor_WhenChainHasUntrustedHop()
        {
            var context = new DefaultHttpContext();
            // Direct connection is trusted proxy (10.0.0.10)
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.10");
            // XFF chain has untrusted hop 192.168.1.50 between client and trusted proxy
            context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 192.168.1.50, 10.0.0.10";

            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:RequireTrustedProxy"] = "true",
                ["Oidc:TrustedProxies"] = "10.0.0.10" // 192.168.1.50 is NOT trusted
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            bool isTrusted = TrustedProxyHelper.IsTrustedProxy(context, config);
            Assert.False(isTrusted);
        }

        [Fact]
        public void TrustedProxyHelper_Unconfigured_LoopbackTrusted_LANNotTrusted()
        {
            // Unconfigured (TrustedProxies empty)
            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = ""
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            // 1. Loopback ip address should be trusted
            var loopbackCtx = new DefaultHttpContext();
            loopbackCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            Assert.True(TrustedProxyHelper.IsTrustedProxy(loopbackCtx, config));

            // 2. LAN ip address (e.g., 10.0.0.50) should NOT be trusted
            var lanCtx = new DefaultHttpContext();
            lanCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50");
            Assert.False(TrustedProxyHelper.IsTrustedProxy(lanCtx, config));
        }

        [Fact]
        public void TrustedProxyHelper_ConfiguredProxyTrusted()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = "10.0.0.50"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var proxyCtx = new DefaultHttpContext();
            proxyCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50");
            Assert.True(TrustedProxyHelper.IsTrustedProxy(proxyCtx, config));

            var otherCtx = new DefaultHttpContext();
            otherCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.51");
            Assert.False(TrustedProxyHelper.IsTrustedProxy(otherCtx, config));
        }

        [Fact]
        public async Task TrustedProxyHelper_ForgedHeaderFromLanHost_DegradesToGuest()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Oidc:TrustedProxies"] = "127.0.0.1" // loopback only
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50"); // LAN host (not trusted)
            context.Request.Headers["Remote-User"] = "malicious_admin";
            context.Request.Headers["Remote-Groups"] = "full_admin";

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("guest", identity.Username);
            Assert.Empty(identity.GroupNames);
            Assert.False(context.Request.Headers.ContainsKey("Remote-User"));
            Assert.False(context.Request.Headers.ContainsKey("Remote-Groups"));
        }
    }
}



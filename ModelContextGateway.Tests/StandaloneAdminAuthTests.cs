using System.Net;
using System.Security.Claims;
using ModelContextGateway.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ModelContextGateway.Tests
{
    public class StandaloneAdminAuthTests
    {
        [Fact]
        [Requirement("AUTH-STANDALONE-LOOPBACK-ALLOW", "AUTH", RequirementType.Positive, "Standalone mode without external IDP grants admin access to loopback IP addresses.")]
        public void IsAdmin_StandaloneMode_LoopbackIp_ReturnsTrue()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var httpContextIpv4 = new DefaultHttpContext();
            httpContextIpv4.Connection.RemoteIpAddress = IPAddress.Loopback; // 127.0.0.1

            var httpContextIpv6 = new DefaultHttpContext();
            httpContextIpv6.Connection.RemoteIpAddress = IPAddress.IPv6Loopback; // ::1

            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(IPAddress.Loopback, config));
            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(IPAddress.IPv6Loopback, config));
            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContextIpv4));
            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContextIpv6));

            var guestIdentity = new UserIdentityContext("guest", "HeaderAuth", new List<string>());
            Assert.True(SecurityValidationHelper.IsAdmin(guestIdentity, config, httpContextIpv4));
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-CUSTOM-CIDR-ALLOW", "AUTH", RequirementType.Positive, "Standalone mode grants admin access to client IPs matching Admin:StandaloneAllowedNetworks CIDR ranges.")]
        public void IsAdmin_StandaloneMode_CustomCidr_ReturnsTrue()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Admin:StandaloneAllowedNetworks:0"] = "10.0.0.0/8",
                ["Admin:StandaloneAllowedNetworks:1"] = "192.168.1.0/24"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var lanIp1 = IPAddress.Parse("10.0.1.50");
            var lanIp2 = IPAddress.Parse("192.168.1.200");

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = lanIp1;

            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(lanIp1, config));
            Assert.True(SecurityValidationHelper.IsStandaloneAdminNetwork(lanIp2, config));
            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContext));
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-EXTERNAL-DENY", "GUARD", RequirementType.Negative, "Standalone mode denies admin access to non-whitelisted external IPs without an Admin AppKey.")]
        public void IsAdmin_StandaloneMode_UntrustedIp_ReturnsFalse()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Admin:StandaloneAllowedNetworks:0"] = "10.0.0.0/8"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var externalIp = IPAddress.Parse("203.0.113.10");

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = externalIp;

            Assert.False(SecurityValidationHelper.IsStandaloneAdminNetwork(externalIp, config));
            Assert.False(SecurityValidationHelper.IsAdmin(null, config, httpContext));

            var guestIdentity = new UserIdentityContext("guest", "HeaderAuth", new List<string>());
            Assert.False(SecurityValidationHelper.IsAdmin(guestIdentity, config, httpContext));
        }

        [Fact]
        [Requirement("AUTH-APPKEY-ADMIN-SCOPE-ALLOW", "AUTH", RequirementType.Positive, "AppKeys with admin scope grant Administrator role and pass AdminPolicy.")]
        public async Task AppKey_WithAdminScope_GrantsAdminAccess()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                ["Admin:GroupSid"] = "S-1-5-32-544"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            // 1. Admin AppKey with scope "admin"
            var adminIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "agent-1"),
                new Claim(ClaimTypes.Role, "McpClient"),
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim("Scope", "admin")
            }, "AppKey");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10"); // External IP

            var adminResult = await authService.AuthorizeAsync(adminPrincipal, httpContext, "AdminPolicy");
            Assert.True(adminResult.Succeeded);

            // 2. Admin AppKey with scope "all" or role Administrator
            var allIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "agent-2"),
                new Claim(ClaimTypes.Role, "McpClient"),
                new Claim(ClaimTypes.Role, "Administrator")
            }, "AppKey");
            var allPrincipal = new ClaimsPrincipal(allIdentity);

            var allResult = await authService.AuthorizeAsync(allPrincipal, httpContext, "AdminPolicy");
            Assert.True(allResult.Succeeded);

            // 3. Regular unprivileged AppKey with scope "tools/read"
            var regularIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "agent-3"),
                new Claim(ClaimTypes.Role, "McpClient"),
                new Claim("Scope", "tools/read")
            }, "AppKey");
            var regularPrincipal = new ClaimsPrincipal(regularIdentity);

            var regularResult = await authService.AuthorizeAsync(regularPrincipal, httpContext, "AdminPolicy");
            Assert.False(regularResult.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-APPKEY-WILDCARD-SCOPE-ALLOW", "AUTH", RequirementType.Positive, "AppKeys with wildcard scope '*' grant Administrator role and pass AdminPolicy.")]
        public async Task AppKey_WithWildcardScope_GrantsAdminAccess()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                ["Admin:GroupSid"] = "S-1-5-32-544"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            var wildcardIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "agent-wildcard"),
                new Claim(ClaimTypes.Role, "McpClient"),
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim("Scope", "admin")
            }, "AppKey");
            var wildcardPrincipal = new ClaimsPrincipal(wildcardIdentity);

            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.50");

            var result = await authService.AuthorizeAsync(wildcardPrincipal, httpContext, "AdminPolicy");
            Assert.True(result.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-ADMINPOLICY-LOOPBACK-ALLOW", "AUTH", RequirementType.Positive, "AdminPolicy succeeds in standalone mode for unauthenticated loopback requests.")]
        public async Task AdminPolicy_StandaloneMode_LoopbackIp_PassesAdminPolicy()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            var result = await authService.AuthorizeAsync(unauthenticatedPrincipal, httpContext, "AdminPolicy");
            Assert.True(result.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-STANDALONE-ADMINPOLICY-EXTERNAL-DENY", "GUARD", RequirementType.Negative, "AdminPolicy rejects unauthenticated requests from non-whitelisted external IPs in standalone mode.")]
        public async Task AdminPolicy_StandaloneMode_ExternalUntrustedIp_FailsAdminPolicy()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

            var result = await authService.AuthorizeAsync(unauthenticatedPrincipal, httpContext, "AdminPolicy");
            Assert.False(result.Succeeded);
        }

        [Fact]
        [Requirement("AUTH-EXTERNAL-IDP-DENIES-ANONYMOUS-LOOPBACK", "GUARD", RequirementType.Negative, "When an external IDP is configured, anonymous loopback requests do not bypass authentication.")]
        public async Task AdminPolicy_ExternalIdpConfigured_LoopbackIp_RequiresCredentials()
        {
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                ["Ldap:Server"] = "ldap.corp.internal"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var sp = services.BuildServiceProvider();
            var authService = sp.GetRequiredService<IAuthorizationService>();

            var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            var result = await authService.AuthorizeAsync(unauthenticatedPrincipal, httpContext, "AdminPolicy");
            Assert.False(result.Succeeded);

            Assert.True(SecurityValidationHelper.HasExternalIdp(config, httpContext));
            Assert.False(SecurityValidationHelper.IsAdmin(null, config, httpContext));
        }

        [Fact]
        [Requirement("AUTH-APPKEY-ITEMS-SCOPE-ALLOW", "AUTH", RequirementType.Positive, "SecurityValidationHelper recognizes admin scopes in HttpContext.Items.")]
        public void IsAdmin_AppKeyScopes_InHttpContextItems_ReturnsTrue()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.99");
            httpContext.Items["AppKeyScopes"] = "[\"admin\"]";
            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContext));

            httpContext.Items["AppKeyScopes"] = "[\"*\"]";
            Assert.True(SecurityValidationHelper.IsAdmin(null, config, httpContext));

            httpContext.Items["AppKeyScopes"] = "[\"all\"]";
            Assert.False(SecurityValidationHelper.IsAdmin(null, config, httpContext));

            httpContext.Items["AppKeyScopes"] = "[\"tools/read\"]";
            Assert.False(SecurityValidationHelper.IsAdmin(null, config, httpContext));
        }
    }
}

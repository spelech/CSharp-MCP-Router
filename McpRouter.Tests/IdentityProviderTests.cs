using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using McpRouter.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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

            var provider = new OidcIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("steve", identity.Username);
            Assert.Equal("PocketID_TinyAuth", identity.AuthenticationType);
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

            var adProvider = new ActiveDirectoryIdentityProvider();
            var oidcProvider = new OidcIdentityProvider();
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

            Assert.False(context.Request.Headers.ContainsKey("Remote-User"));
            Assert.False(context.Request.Headers.ContainsKey("Remote-Groups"));
            Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-User"));
            Assert.False(context.Request.Headers.ContainsKey("sso_groups"));
            Assert.Equal("guest", identity.Username);
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
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.10" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.True(context.Request.Headers.ContainsKey("Remote-User"));
            Assert.Equal("steve", identity.Username);
            Assert.Contains("house_member", identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_MapsAdminSid_ForAdminGroups()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            context.Request.Headers["Remote-User"] = "steve";
            context.Request.Headers["Remote-Groups"] = "full_admin";

            var provider = new OidcIdentityProvider();
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Contains("S-1-5-32-544", identity.AllSids);
        }
    }
}

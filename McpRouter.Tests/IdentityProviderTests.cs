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
        public async Task OidcIdentityProvider_WithProxyValidation_Allows_TrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
            context.Request.Headers["Remote-User"] = "admin-user";
            context.Request.Headers["Remote-Groups"] = "Administrators";

            var configDict = new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.5, 10.0.0.6" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            Assert.Equal("admin-user", identity.Username);
            Assert.Contains("Administrators", identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_WithProxyValidation_Rejects_UntrustedProxy()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");
            context.Request.Headers["Remote-User"] = "admin-user";
            context.Request.Headers["Remote-Groups"] = "Administrators";

            var configDict = new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "true" },
                { "Oidc:TrustedProxies", "10.0.0.5, 10.0.0.6" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new OidcIdentityProvider(config);
            var identity = await provider.ResolveIdentityAsync(context);

            // Since request is from an untrusted proxy, sso headers should be ignored, falling back to guest
            Assert.Equal("guest", identity.Username);
            Assert.Empty(identity.GroupNames);
        }

        [Fact]
        public async Task OidcIdentityProvider_Parses_Remote_User_And_Groups_Headers()
        {
            var context = new DefaultHttpContext();
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
            context.Request.Headers["Remote-User"] = "alex";
            context.Request.Headers["sso_groups"] = "dev_team";

            var adProvider = new ActiveDirectoryIdentityProvider();
            var oidcProvider = new OidcIdentityProvider();
            var composite = new CompositeIdentityProvider(new IIdentityProvider[] { adProvider, oidcProvider });

            var identity = await composite.ResolveIdentityAsync(context);

            Assert.Equal("alex", identity.Username);
            Assert.Contains("dev_team", identity.GroupNames);
        }
    }
}

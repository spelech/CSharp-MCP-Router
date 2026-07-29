using System.Security.Principal;
using System.Threading.Tasks;
using McpRouter.Core.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace McpRouter.Tests
{
    public class IdentityProviderTests
    {
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

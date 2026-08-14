using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class CompositeIdentityProviderTests
    {
        [Fact]
        public void ProviderName_ReturnsComposite()
        {
            var provider = new CompositeIdentityProvider(new List<IIdentityProvider>());
            Assert.Equal("Composite", provider.ProviderName);
        }

        [Fact]
        public async Task ResolveIdentityAsync_ReturnsFirstNonAnonymousUser()
        {
            var p1 = new Mock<IIdentityProvider>();
            p1.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
              .ReturnsAsync(new UserIdentityContext("anonymous", "P1", new List<string>()));

            var p2 = new Mock<IIdentityProvider>();
            p2.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
              .ReturnsAsync(new UserIdentityContext("steve", "P2", new List<string> { "full_admin" }));

            var composite = new CompositeIdentityProvider(new[] { p1.Object, p2.Object });
            var httpContext = new DefaultHttpContext();

            var result = await composite.ResolveIdentityAsync(httpContext);

            Assert.Equal("steve", result.Username);
            Assert.Equal("P2", result.AuthenticationType);
        }

        [Fact]
        public async Task ResolveIdentityAsync_FallsBackToAnonymous_WhenNoUserResolved()
        {
            var p1 = new Mock<IIdentityProvider>();
            p1.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
              .ReturnsAsync(new UserIdentityContext("anonymous", "P1", new List<string>()));

            var composite = new CompositeIdentityProvider(new[] { p1.Object });
            var httpContext = new DefaultHttpContext();

            var result = await composite.ResolveIdentityAsync(httpContext);

            Assert.Equal("anonymous", result.Username);
            Assert.Equal("Composite", result.AuthenticationType);
        }

        [Fact]
        public async Task ResolveIdentityAsync_FallsBackToOidcProvider_WhenAnonymous()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Oidc:RequireTrustedProxy", "false" },
                { "Oidc:TrustedProxies", "127.0.0.1" }
            }).Build();

            var p1 = new Mock<IIdentityProvider>();
            p1.Setup(p => p.ResolveIdentityAsync(It.IsAny<HttpContext>()))
              .ReturnsAsync(new UserIdentityContext("anonymous", "P1", new List<string>()));

            var oidc = new OidcIdentityProvider(config);
            var composite = new CompositeIdentityProvider(new IIdentityProvider[] { p1.Object, oidc });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            httpContext.Request.Headers["Remote-User"] = "oidc_user";
            httpContext.Request.Headers["Remote-Groups"] = "house_member";

            var result = await composite.ResolveIdentityAsync(httpContext);

            Assert.Equal("oidc_user", result.Username);
        }
    }
}

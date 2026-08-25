using System.Security.Claims;
using System.Security.Principal;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ModelContextGateway.Tests
{
    public class ActiveDirectoryWindowsIdentityTests
    {
        [Fact]
        [Requirement("AUTH-04", "AUTH", RequirementType.Positive, "ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP")]
        public async Task ResolveIdentityAsync_ExtractsWindowsIdentitySids_ViaAccessor()
        {
            var mockWindowsAccessor = new Mock<IWindowsIdentityAccessor>();
            var userSid = "S-1-5-21-111111111-222222222-333333333-1001";
            var groupSids = new List<string> { "S-1-5-32-544", "S-1-5-21-111111111-222222222-333333333-512" };

            var identity = new GenericIdentity("DOMAIN\\john.doe");
            mockWindowsAccessor.Setup(w => w.TryGetWindowsIdentityDetails(identity, out userSid, out groupSids))
                .Returns(true);

            var configDict = new Dictionary<string, string?>
            {
                { "Security:AllowedIpRanges:0", "127.0.0.1/32" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new ActiveDirectoryIdentityProvider(
                configuration: config,
                ldapService: null,
                authRepo: null,
                windowsIdentityAccessor: mockWindowsAccessor.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            httpContext.User = new ClaimsPrincipal(identity);

            var context = await provider.ResolveIdentityAsync(httpContext);

            context.Should().NotBeNull();
            context.Username.Should().Be("DOMAIN\\john.doe");
            context.Sid.Should().Be("S-1-5-21-111111111-222222222-333333333-1001");
            context.GroupNames.Should().Contain("S-1-5-32-544");
            context.Sids.Should().Contain("S-1-5-21-111111111-222222222-333333333-1001");
            context.Sids.Should().Contain("S-1-5-32-544");
        }

        [Fact]
        [Requirement("AUTH-04", "AUTH", RequirementType.Positive, "ActiveDirectoryIdentityProvider extracts Windows caller SIDs and security groups via IWindowsIdentityAccessor and augments with LDAP")]
        public async Task ResolveIdentityAsync_AugmentsWithLdapSids_WhenLdapServiceProvided()
        {
            var mockWindowsAccessor = new Mock<IWindowsIdentityAccessor>();
            string? userSid = "S-1-5-21-1001";
            var groupSids = new List<string> { "S-1-5-32-545" };

            var identity = new GenericIdentity("alice");
            mockWindowsAccessor.Setup(w => w.TryGetWindowsIdentityDetails(identity, out userSid, out groupSids))
                .Returns(true);

            var mockLdap = new Mock<ILdapService>();
            mockLdap.Setup(l => l.ResolveUserSidsAsync("alice"))
                .ReturnsAsync(new List<string> { "S-1-5-21-9999", "S-1-5-32-544" });

            var configDict = new Dictionary<string, string?>
            {
                { "Security:AllowedIpRanges:0", "127.0.0.1/32" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var provider = new ActiveDirectoryIdentityProvider(
                configuration: config,
                ldapService: mockLdap.Object,
                authRepo: null,
                windowsIdentityAccessor: mockWindowsAccessor.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            httpContext.User = new ClaimsPrincipal(identity);

            var context = await provider.ResolveIdentityAsync(httpContext);

            context.Should().NotBeNull();
            context.Sids.Should().Contain(new[] { "S-1-5-21-1001", "S-1-5-32-545", "S-1-5-21-9999", "S-1-5-32-544" });
        }
    }
}

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using McpRouter.Extensions;

namespace McpRouter.Tests
{
    public class AdminPolicySidOnlyTests
    {
        /// <summary>
        /// Verifies that users with role names but lacking explicit Admin SID claim are denied by AdminPolicy.
        /// </summary>
        [Fact]
        [Requirement("AUTH-01", "AdminPolicy must require explicit Admin SID claim and reject role-only principals", Type = RequirementType.Negative, Category = "AUTH")]
        public async Task AdminPolicy_Denies_AdministratorOrFullAdminRoles_Without_AdminSid()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupSid", "S-1-5-32-544" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            // Build HttpContext with request services to simulate resource
            var httpContext = new DefaultHttpContext();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

            // Principal with only role name Administrator
            var identityWithAdminRole = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");
            var principalWithAdminRole = new ClaimsPrincipal(identityWithAdminRole);

            // Principal with only role name full_admin
            var identityWithFullAdminRole = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "full_admin"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");
            var principalWithFullAdminRole = new ClaimsPrincipal(identityWithFullAdminRole);

            // Act & Assert
            var resultAdminRole = await authService.AuthorizeAsync(principalWithAdminRole, httpContext, "AdminPolicy");
            Assert.False(resultAdminRole.Succeeded);

            var resultFullAdminRole = await authService.AuthorizeAsync(principalWithFullAdminRole, httpContext, "AdminPolicy");
            Assert.False(resultFullAdminRole.Succeeded);
        }

        /// <summary>
        /// Verifies that principals with the configured Admin SID are granted administrative policy access.
        /// </summary>
        [Fact]
        [Requirement("AUTH-01", "Admin SID authorizes administrative access across all protected router endpoints", Type = RequirementType.Positive, Category = "AUTH")]
        public async Task AdminPolicy_Allows_Principal_With_AdminSid()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

            var configDict = new Dictionary<string, string?>
            {
                { "Admin:GroupSid", "S-1-5-32-544" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddMcpOpenIddict(mockEnv.Object, config);

            var httpContext = new DefaultHttpContext();
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

            // Principal with Admin SID claim
            var identityWithSid = new ClaimsIdentity(new[]
            {
                new Claim("Sid", "S-1-5-32-544"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");
            var principalWithSid = new ClaimsPrincipal(identityWithSid);

            // Act
            var result = await authService.AuthorizeAsync(principalWithSid, httpContext, "AdminPolicy");

            // Assert
            Assert.True(result.Succeeded);
        }
    }
}

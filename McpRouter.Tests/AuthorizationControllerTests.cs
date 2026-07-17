using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using McpRouter.Controllers;
using Moq;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace McpRouter.Tests
{
    public class AuthorizationControllerTests
    {
        [Fact]
        public async Task RegisterClient_Should_Return_Client_Credentials()
        {
            // Arrange
            var mockManager = new Mock<IOpenIddictApplicationManager>();
            mockManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), default))
                .ReturnsAsync(new object()); // Mock returns something

            var controller = new AuthorizationController(mockManager.Object);

            var metadataJson = @"{ ""client_name"": ""Test Client"" }";
            using var doc = JsonDocument.Parse(metadataJson);

            // Act
            var result = await controller.RegisterClient(doc.RootElement);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            
            var response = okResult!.Value;
            response.Should().NotBeNull();
            
            // Check that client_id and client_secret are generated
            var type = response!.GetType();
            var clientId = type.GetProperty("client_id")?.GetValue(response, null);
            var clientSecret = type.GetProperty("client_secret")?.GetValue(response, null);

            clientId.Should().NotBeNull();
            clientSecret.Should().NotBeNull();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using McpRouter.Controllers;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Microsoft.AspNetCore.Http;

namespace McpRouter.Tests
{
    public class ClientsControllerTests
    {
        [Fact]
        public async Task CreateClient_ReturnsOk_WithGeneratedCredentials()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            mockAppManager.Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new object()); // Just returns a dummy object for the app

            var controller = new ClientsController(mockAppManager.Object);
            var model = new ClientsController.CreateClientModel
            {
                DisplayName = "Test CLI",
                Scopes = new List<string> { "custom_scope" }
            };

            // Act
            var result = await controller.CreateClient(model);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;
            value.Should().NotBeNull();
            
            var clientIdProp = value!.GetType().GetProperty("ClientId")?.GetValue(value, null) as string;
            var clientSecretProp = value!.GetType().GetProperty("ClientSecret")?.GetValue(value, null) as string;
            var displayNameProp = value!.GetType().GetProperty("DisplayName")?.GetValue(value, null) as string;

            clientIdProp.Should().NotBeNullOrEmpty();
            clientSecretProp.Should().NotBeNullOrEmpty();
            displayNameProp.Should().Be("Test CLI");
            
            mockAppManager.Verify(m => m.CreateAsync(It.Is<OpenIddictApplicationDescriptor>(d => 
                d.DisplayName == "Test CLI" && 
                d.Permissions.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "custom_scope") &&
                d.Permissions.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + "mcp_client")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteClient_ReturnsNoContent_WhenAppExists()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var dummyApp = new object();
            mockAppManager.Setup(m => m.FindByIdAsync("123", It.IsAny<CancellationToken>()))
                          .ReturnsAsync(dummyApp);
            mockAppManager.Setup(m => m.DeleteAsync(dummyApp, It.IsAny<CancellationToken>()))
                          .Returns(ValueTask.CompletedTask);

            var controller = new ClientsController(mockAppManager.Object);

            // Act
            var result = await controller.DeleteClient("123");

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockAppManager.Verify(m => m.DeleteAsync(dummyApp, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteClient_ReturnsNotFound_WhenAppDoesNotExist()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            mockAppManager.Setup(m => m.FindByIdAsync("123", It.IsAny<CancellationToken>()))
                          .ReturnsAsync((object?)null);

            var controller = new ClientsController(mockAppManager.Object);

            // Act
            var result = await controller.DeleteClient("123");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}

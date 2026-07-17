using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public async Task GetClients_ReturnsOk_WithClientsAndMappedProperties()
        {
            // Arrange
            var mockAppManager = new Mock<IOpenIddictApplicationManager>();
            var app1 = new object();
            var app2 = new object();
            var appsList = new List<object> { app1, app2 };

            // Setup ListAsync
            mockAppManager.Setup(m => m.ListAsync(null, null, It.IsAny<CancellationToken>()))
                          .Returns(ToAsyncEnumerable(appsList));

            // Setup properties for app1 (dynamic client)
            mockAppManager.Setup(m => m.GetIdAsync(app1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("id-1");
            mockAppManager.Setup(m => m.GetClientIdAsync(app1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("client-1");
            mockAppManager.Setup(m => m.GetDisplayNameAsync(app1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("Client One");
            mockAppManager.Setup(m => m.GetPermissionsAsync(app1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(ImmutableArray.Create("scp:mcp_client", "scp:dynamic_client"));

            // Setup properties for app2 (manual client)
            mockAppManager.Setup(m => m.GetIdAsync(app2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("id-2");
            mockAppManager.Setup(m => m.GetClientIdAsync(app2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("client-2");
            mockAppManager.Setup(m => m.GetDisplayNameAsync(app2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync("Client Two");
            mockAppManager.Setup(m => m.GetPermissionsAsync(app2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(ImmutableArray.Create("scp:mcp_client", "scp:custom_scope"));

            var controller = new ClientsController(mockAppManager.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.GetClients();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value as IEnumerable<object>;
            value.Should().NotBeNull();
            
            var list = value!.ToList();
            list.Should().HaveCount(2);

            // Verify first client (dynamic)
            var client1 = list[0];
            client1.GetType().GetProperty("Id")?.GetValue(client1).Should().Be("id-1");
            client1.GetType().GetProperty("ClientId")?.GetValue(client1).Should().Be("client-1");
            client1.GetType().GetProperty("DisplayName")?.GetValue(client1).Should().Be("Client One");
            
            var scopes1 = client1.GetType().GetProperty("Scopes")?.GetValue(client1) as IEnumerable<string>;
            scopes1.Should().NotBeNull();
            scopes1.Should().Contain("mcp_client");
            scopes1.Should().Contain("dynamic_client");
            client1.GetType().GetProperty("IsDynamic")?.GetValue(client1).Should().Be(true);

            // Verify second client (manual)
            var client2 = list[1];
            client2.GetType().GetProperty("Id")?.GetValue(client2).Should().Be("id-2");
            client2.GetType().GetProperty("ClientId")?.GetValue(client2).Should().Be("client-2");
            client2.GetType().GetProperty("DisplayName")?.GetValue(client2).Should().Be("Client Two");
            
            var scopes2 = client2.GetType().GetProperty("Scopes")?.GetValue(client2) as IEnumerable<string>;
            scopes2.Should().NotBeNull();
            scopes2.Should().Contain("mcp_client");
            scopes2.Should().Contain("custom_scope");
            client2.GetType().GetProperty("IsDynamic")?.GetValue(client2).Should().Be(false);
        }

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

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }
    }
}

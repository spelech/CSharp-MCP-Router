using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using McpRouter.Core.Routing;
using McpRouter.Services;
using Xunit;

namespace McpRouter.Tests
{
    public class SemanticSearchServiceTests
    {
        [Fact]
        public async Task SearchToolsSemanticAsync_GracefullyHandlesEmbeddingExceptions_AndLogsWarning()
        {
            // Arrange
            var query = "test query";
            var tools = new List<object>
            {
                new { name = "tool1", description = "tool1 desc" }
            };

            var embeddingMock = new Mock<IEmbeddingService>();

            // Mock query embedding success but tool text embedding failure
            embeddingMock.Setup(e => e.GetEmbeddingAsync("test query"))
                .ReturnsAsync(new float[] { 0.1F, 0.2F });

            embeddingMock.Setup(e => e.GetEmbeddingAsync("tool1: tool1 desc"))
                .ThrowsAsync(new Exception("API limit reached or service unavailable"));

            var loggerMock = new Mock<ILogger>();

            // Act
            // If the code works correctly, it should log a warning and return empty/default search results
            // rather than failing the whole request.
            var results = await SemanticSearchService.SearchToolsSemanticAsync(query, tools, embeddingMock.Object, loggerMock.Object);

            // Assert
            Assert.NotNull(results);

            // Verify that logger.Log got called with a LogLevel.Warning
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to generate embedding")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}

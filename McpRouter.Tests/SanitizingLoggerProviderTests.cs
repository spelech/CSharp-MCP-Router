using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using McpRouter.Core.Logging;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class SanitizingLoggerProviderTests
    {
        [Fact]
        public void SanitizingLoggerProvider_RedactsBearerTokensAndKeys()
        {
            // Arrange
            var mockInnerLogger = new Mock<ILogger>();
            string loggedMessage = "";
            mockInnerLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, id, state, ex, formatter) =>
                {
                    loggedMessage = state.ToString() ?? "";
                });

            var mockInnerProvider = new Mock<ILoggerProvider>();
            mockInnerProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(mockInnerLogger.Object);

            var sanitizingProvider = new SanitizingLoggerProvider(mockInnerProvider.Object);
            var logger = sanitizingProvider.CreateLogger("TestCategory");

            // Act - Log message containing bearer token and key
            logger.LogInformation("This is a secret: Bearer some_token_123_abc and \"api-key\": \"my-secret-key\"");

            // Assert
            Assert.Contains("[REDACTED]", loggedMessage);
            Assert.DoesNotContain("some_token_123_abc", loggedMessage);
            Assert.DoesNotContain("my-secret-key", loggedMessage);
        }

        [Fact]
        public void SanitizingLoggerProvider_LeavesPlainMessagesUnchanged()
        {
            // Arrange
            var mockInnerLogger = new Mock<ILogger>();
            string loggedMessage = "";
            mockInnerLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, id, state, ex, formatter) =>
                {
                    loggedMessage = state.ToString() ?? "";
                });

            var mockInnerProvider = new Mock<ILoggerProvider>();
            mockInnerProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(mockInnerLogger.Object);

            var sanitizingProvider = new SanitizingLoggerProvider(mockInnerProvider.Object);
            var logger = sanitizingProvider.CreateLogger("TestCategory");

            // Act
            logger.LogInformation("Standard log statement with no secrets.");

            // Assert
            Assert.Equal("Standard log statement with no secrets.", loggedMessage);
        }

        [Fact]
        public void SanitizingLoggerProvider_RedactsSecretsInExceptionMessageAndToString()
        {
            // Arrange
            var mockInnerLogger = new Mock<ILogger>();
            Exception? loggedException = null;
            mockInnerLogger
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, id, state, ex, formatter) =>
                {
                    loggedException = ex;
                });

            var mockInnerProvider = new Mock<ILoggerProvider>();
            mockInnerProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(mockInnerLogger.Object);

            var sanitizingProvider = new SanitizingLoggerProvider(mockInnerProvider.Object);
            var logger = sanitizingProvider.CreateLogger("TestCategory");

            var originalException = new InvalidOperationException("Failed authorization with Bearer my_super_secret_token");

            // Act
            logger.LogError(originalException, "An error occurred");

            // Assert
            Assert.NotNull(loggedException);
            Assert.Contains("[REDACTED]", loggedException.Message);
            Assert.Contains("[REDACTED]", loggedException.ToString());
            Assert.DoesNotContain("my_super_secret_token", loggedException.Message);
            Assert.DoesNotContain("my_super_secret_token", loggedException.ToString());
        }
    }
}

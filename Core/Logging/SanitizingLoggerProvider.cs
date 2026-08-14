using System;
using Microsoft.Extensions.Logging;

namespace McpRouter.Core.Logging
{
    public class SanitizingLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerProvider _inner;

        public SanitizingLoggerProvider(ILoggerProvider inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _inner.CreateLogger(categoryName);
            return new SanitizingLogger(innerLogger);
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        private class SanitizingLogger : ILogger
        {
            private readonly ILogger _innerLogger;

            public SanitizingLogger(ILogger innerLogger)
            {
                _innerLogger = innerLogger;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return _innerLogger.BeginScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _innerLogger.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (formatter == null)
                {
                    _innerLogger.Log(logLevel, eventId, state, exception, (s, e) => "");
                    return;
                }

                // Format the message with the original formatter
                string formattedMessage = formatter(state, exception);

                // Sanitize the message using PiiSanitizer
                string sanitizedMessage = PiiSanitizer.SanitizePayload(formattedMessage);

                // Sanitize the exception if present to redact tokens/passwords/keys in message or stacktrace
                Exception? sanitizedException = null;
                if (exception != null)
                {
                    sanitizedException = new SanitizedException(exception);
                }

                // Forward to the inner logger
                _innerLogger.Log<string>(logLevel, eventId, sanitizedMessage, sanitizedException, (s, e) => s);
            }
        }

        private class SanitizedException : Exception
        {
            private readonly string _message;
            private readonly string _toString;

            public SanitizedException(Exception original) : base(original.Message)
            {
                _message = PiiSanitizer.SanitizePayload(original.Message);
                _toString = PiiSanitizer.SanitizePayload(original.ToString());
            }

            public override string Message => _message;
            public override string ToString() => _toString;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace McpRouter.Services
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }

    public static class LogBuffer
    {
        private static readonly ConcurrentQueue<LogEntry> _queue = new();
        private const int MaxLogs = 200;

        public static void Add(LogLevel level, string category, string message, Exception? exception)
        {
            _queue.Enqueue(new LogEntry
            {
                Level = level,
                Category = category,
                Message = message,
                Exception = exception?.ToString()
            });

            while (_queue.Count > MaxLogs)
            {
                _queue.TryDequeue(out _);
            }
        }

        public static List<LogEntry> GetLogs()
        {
            return new List<LogEntry>(_queue);
        }
        
        public static void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
        }
    }

    public class InMemoryLogger : ILogger
    {
        private readonly string _categoryName;

        public InMemoryLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LogBuffer.Add(logLevel, _categoryName, message, exception);
        }
    }

    public class InMemoryLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(categoryName);
        public void Dispose() { }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpRouter.Core.Secrets;
using McpRouter.Core.Transports;
using McpRouter.Models;
using Moq;
using Xunit;

namespace McpRouter.Tests
{
    public class StdioTransportTests
    {
        public class CapturingLogger : ILogger
        {
            public List<string> LoggedMessages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var msg = formatter(state, exception);
                LoggedMessages.Add($"{logLevel}: {msg}");
            }
        }

        private string GetMockScriptPath()
        {
            var paths = new[]
            {
                "mock_stdio.js",
                "McpRouter.Tests/mock_stdio.js",
                "../McpRouter.Tests/mock_stdio.js",
                "../../McpRouter.Tests/mock_stdio.js",
                "../../../McpRouter.Tests/mock_stdio.js",
                "../../../../McpRouter.Tests/mock_stdio.js"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            throw new FileNotFoundException("Could not find mock_stdio.js");
        }

        [Fact]
        public async Task StdioTransport_ShouldInitializeAndCallToolSuccessfully()
        {
            var scriptPath = GetMockScriptPath();
            var server = new McpServer
            {
                Id = "stdio-test",
                Url = $"node \"{scriptPath}\"",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await transport.ConnectAsync();

            var messagesReceived = new List<JsonRpcMessage>();
            transport.StartReader(async msg =>
            {
                lock (messagesReceived)
                {
                    messagesReceived.Add(msg);
                }
                await Task.CompletedTask;
            });

            // Send initialize request
            var initReq = "{\"jsonrpc\":\"2.0\",\"id\":\"init-1\",\"method\":\"initialize\",\"params\":{}}";
            var initResp = await transport.SendRequestAsync("initialize", initReq);
            Assert.NotNull(initResp);
            Assert.Null(initResp.Error);

            // Send tools/list request
            var listReq = "{\"jsonrpc\":\"2.0\",\"id\":\"list-1\",\"method\":\"tools/list\",\"params\":{}}";
            var listResp = await transport.SendRequestAsync("tools/list", listReq);
            Assert.NotNull(listResp);
            Assert.Null(listResp.Error);

            // Send tools/call request
            var callReq = "{\"jsonrpc\":\"2.0\",\"id\":\"call-1\",\"method\":\"tools/call\",\"params\":{\"name\":\"echo\",\"arguments\":{\"message\":\"hello stdio\"}}}";
            var callResp = await transport.SendRequestAsync("tools/call", callReq);
            Assert.NotNull(callResp);
            Assert.Null(callResp.Error);
            Assert.NotNull(callResp.Result);

            var resultStr = callResp.Result.ToString();
            Assert.Contains("hello stdio", resultStr);
        }

        [Fact]
        public async Task StdioTransport_ShouldThrowSecurityExceptionForUnsafeExecutable()
        {
            var server = new McpServer
            {
                Id = "unsafe-test",
                Url = "node; rm -rf /",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await Assert.ThrowsAsync<System.Security.SecurityException>(() => transport.ConnectAsync());
        }

        [Fact]
        public async Task StdioTransport_ShouldThrowSecurityExceptionForShellExecutable()
        {
            var server = new McpServer
            {
                Id = "shell-test",
                Url = "bash my_script.sh",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await Assert.ThrowsAsync<System.Security.SecurityException>(() => transport.ConnectAsync());
        }

        [Fact]
        public async Task StdioTransport_ShouldThrowOnInvalidExecutable()
        {
            var server = new McpServer
            {
                Id = "invalid-test",
                Url = "non_existent_program_xyz_123",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transport.ConnectAsync());
        }

        [Fact]
        public async Task StdioTransport_ShouldRouteStderrToLogs()
        {
            var scriptPath = GetMockScriptPath();
            var server = new McpServer
            {
                Id = "stdio-test-stderr",
                Url = $"node \"{scriptPath}\"",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await transport.ConnectAsync();
            transport.StartReader(msg => Task.CompletedTask);

            // Call initialize first to establish session
            var initReq = "{\"jsonrpc\":\"2.0\",\"id\":\"init-1\",\"method\":\"initialize\",\"params\":{}}";
            await transport.SendRequestAsync("initialize", initReq);

            // Call stderr_tool to trigger stderr write in script
            var callReq = "{\"jsonrpc\":\"2.0\",\"id\":\"call-stderr\",\"method\":\"tools/call\",\"params\":{\"name\":\"stderr_tool\"}}";
            var resp = await transport.SendRequestAsync("tools/call", callReq);
            Assert.NotNull(resp);

            // Wait a small delay for stderr reader thread to flush to logs
            await Task.Delay(500);

            var warnings = logger.LoggedMessages.Where(m => m.Contains("Warning")).ToList();
            Assert.NotEmpty(warnings);
            Assert.Contains(warnings, w => w.Contains("LOG_FROM_STDERR_TOOL"));
        }

        [Fact]
        public async Task StdioTransport_ShouldTimeoutOnSlowRequests()
        {
            var scriptPath = GetMockScriptPath();
            var server = new McpServer
            {
                Id = "stdio-test-timeout",
                Url = $"node \"{scriptPath}\"",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);
            transport.RequestTimeout = TimeSpan.FromMilliseconds(500); // short timeout

            await transport.ConnectAsync();
            transport.StartReader(msg => Task.CompletedTask);

            var initReq = "{\"jsonrpc\":\"2.0\",\"id\":\"init-1\",\"method\":\"initialize\",\"params\":{}}";
            await transport.SendRequestAsync("initialize", initReq);

            var callReq = "{\"jsonrpc\":\"2.0\",\"id\":\"call-slow\",\"method\":\"tools/call\",\"params\":{\"name\":\"slow_tool\"}}";

            await Assert.ThrowsAsync<TimeoutException>(() => transport.SendRequestAsync("tools/call", callReq));
        }

        [Fact]
        public async Task StdioTransport_ShouldSupportCancellationAndProcessTreeTermination()
        {
            var scriptPath = GetMockScriptPath();
            var server = new McpServer
            {
                Id = "stdio-test-cancel",
                Url = $"node \"{scriptPath}\"",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            System.Diagnostics.Process proc;
            using (var transport = new StdioTransport(server, logger, stateManager))
            {
                await transport.ConnectAsync();
                transport.StartReader(msg => Task.CompletedTask);

                // Get internal process using reflection for assertion
                var procField = typeof(StdioTransport).GetField("_process", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(procField);
                proc = (System.Diagnostics.Process)procField.GetValue(transport)!;
                Assert.NotNull(proc);
                Assert.False(proc.HasExited);
            }

            // After disposing, the process should be killed/terminated
            await Task.Delay(500);
            Assert.True(proc.HasExited);
        }

        [Fact]
        public async Task StdioTransport_ShouldHandleUnexpectedExit()
        {
            var scriptPath = GetMockScriptPath();
            var server = new McpServer
            {
                Id = "stdio-test-exit",
                Url = $"node \"{scriptPath}\"",
                Type = "stdio",
                Enabled = true
            };

            var logger = new CapturingLogger();
            var stateManager = new JsonRpcStateManager();
            using var transport = new StdioTransport(server, logger, stateManager);

            await transport.ConnectAsync();
            transport.StartReader(msg => Task.CompletedTask);

            // Setup a pending request
            var tcs = stateManager.CreateRequest("pending-request-id");

            // Kill process unexpectedly
            var procField = typeof(StdioTransport).GetField("_process", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var proc = (System.Diagnostics.Process)procField!.GetValue(transport)!;
            proc.Kill();

            // Wait a bit for EOF and HandleProcessExit
            await Task.Delay(500);

            // The pending request should have been cancelled/faulted because state manager was cleared on exit
            Assert.True(tcs.Task.IsCanceled || tcs.Task.IsFaulted || tcs.Task.IsCompleted);
        }

        [Fact]
        public void StdioTransport_ParseCommandLine_Handles_Quotes_And_Spaces()
        {
            var cmd = "node \"/path to/script.js\" --arg='val' plain_arg";
            var parsed = StdioTransport.ParseCommandLine(cmd);

            Assert.Equal(4, parsed.Count);
            Assert.Equal("node", parsed[0]);
            Assert.Equal("/path to/script.js", parsed[1]);
            Assert.Equal("--arg=val", parsed[2]);
            Assert.Equal("plain_arg", parsed[3]);
        }
    }
}

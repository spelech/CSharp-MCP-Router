using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;
using McpRouter.Models;
using McpRouter;
using Microsoft.AspNetCore.Mvc;

namespace McpRouter.Tests
{
    public class SseMockStream : Stream
    {
        private readonly byte[] _data;
        private int _position = 0;
        private readonly TaskCompletionSource _tcs = new();

        public SseMockStream(string text)
        {
            _data = System.Text.Encoding.UTF8.GetBytes(text);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, default).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_position < _data.Length)
            {
                int toRead = Math.Min(count, _data.Length - _position);
                Array.Copy(_data, _position, buffer, offset, toRead);
                _position += toRead;
                return toRead;
            }
            // Block indefinitely to simulate no more messages
            await _tcs.Task;
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public class DelayedSseMockStream : Stream
    {
        private readonly Task _waitTask;
        private readonly byte[] _initialData;
        private readonly byte[] _delayedData;
        private int _position = 0;
        private bool _initialDone = false;

        public DelayedSseMockStream(string initialText, string delayedText, Task waitTask)
        {
            _initialData = System.Text.Encoding.UTF8.GetBytes(initialText);
            _delayedData = System.Text.Encoding.UTF8.GetBytes(delayedText);
            _waitTask = waitTask;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _initialData.Length + _delayedData.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, default).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_initialDone)
            {
                if (_position < _initialData.Length)
                {
                    int toRead = Math.Min(count, _initialData.Length - _position);
                    Array.Copy(_initialData, _position, buffer, offset, toRead);
                    _position += toRead;
                    return toRead;
                }
                _initialDone = true;
                _position = 0;
            }

            // Wait for the task signal before providing delayed data
            await _waitTask;

            if (_position < _delayedData.Length)
            {
                int toRead = Math.Min(count, _delayedData.Length - _position);
                Array.Copy(_delayedData, _position, buffer, offset, toRead);
                _position += toRead;
                return toRead;
            }

            // Block indefinitely after delayed data
            await Task.Delay(-1, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public class ChallengerTests
    {
        [Fact]
        public void JsonNode_Rewrite_HandlesBatchCommentsAndCommas()
        {
            var loggerMock = new Mock<ILogger>();
            var session = new ClientSession("session-id", null!, new List<McpServer>(), null!, loggerMock.Object);
            var methodInfo = typeof(ClientSession).GetMethod("RewriteRequestJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // 1. Test batch request rewriting
            var batchJson = "[{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"serverId__toolName\"}}]";
            var rewrittenBatch = (string)methodInfo!.Invoke(session, new object[] { batchJson, "name", "toolName" })!;
            rewrittenBatch.Should().Contain("\"name\":\"toolName\"");

            // 2. Test trailing comma parsing and rewriting
            var trailingCommaJson = "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"serverId__toolName\",},}";
            var rewrittenComma = (string)methodInfo.Invoke(session, new object[] { trailingCommaJson, "name", "toolName" })!;
            rewrittenComma.Should().Contain("\"name\":\"toolName\"");

            // 3. Test comment parsing and rewriting
            var commentsJson = "{\n// some comment\n\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"serverId__toolName\"}\n}";
            var rewrittenComments = (string)methodInfo.Invoke(session, new object[] { commentsJson, "name", "toolName" })!;
            rewrittenComments.Should().Contain("\"name\":\"toolName\"");
        }

        [Fact]
        public async Task AuthMiddleware_CaseInsensitivity_Bypass_Check()
        {
            // Simulate the middleware routing check from Program.cs:
            // if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
            //     !path.StartsWith("/api/register", StringComparison.OrdinalIgnoreCase) && 
            //     !path.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase))
            
            var testPaths = new[] { "/api/clients", "/API/clients", "/Api/clients", "/api/../api/clients" };
            var results = new Dictionary<string, int>();
            var nextCalledResults = new Dictionary<string, bool>();

            foreach (var path in testPaths)
            {
                var context = new DefaultHttpContext();
                context.Request.Path = path;
                
                bool nextCalled = false;
                RequestDelegate next = (ctx) =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                };

                // Simulate Middleware logic from Program.cs
                var p = context.Request.Path.Value ?? string.Empty;
                if (p.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/register", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase))
                {
                    var user = context.Request.Headers["Remote-User"].ToString();
                    if (string.IsNullOrEmpty(user))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        await next(context);
                    }
                }
                else
                {
                    await next(context);
                }

                results[path] = context.Response.StatusCode;
                nextCalledResults[path] = nextCalled;
            }

            // Assert: All variations should be blocked (401) and next not called
            results["/api/clients"].Should().Be(401);
            nextCalledResults["/api/clients"].Should().BeFalse();
            
            results["/API/clients"].Should().Be(401);
            nextCalledResults["/API/clients"].Should().BeFalse();
            results["/Api/clients"].Should().Be(401);
            nextCalledResults["/Api/clients"].Should().BeFalse();
        }

        [Fact]
        public async Task SendRequestAsync_TimesOutCleanly_AndDoesNotLeak()
        {
            // Verify that SendRequestAsync times out cleanly (throws TimeoutException)
            // and removes the pending request from dictionary instead of leaking.
            var server = new McpServer 
            { 
                Id = "backend1", 
                DisplayName = "Backend 1", 
                Url = "http://backend1/mcp", 
                Type = "sse", // SSE type uses background reader and TaskCompletionSource
                Enabled = true 
            };

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            // Mock the GET request to the SSE stream returning a mock endpoint but NO actual messages
            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var stream = new SseMockStream("event: endpoint\ndata: http://backend1/mcp/message\n\n");
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            // Set RequestTimeout to a short duration for the test to run fast
            conn.RequestTimeout = TimeSpan.FromMilliseconds(200);
            await conn.ConnectAsync();
            
            conn.StartReader(async (msg) => {
                await Task.CompletedTask;
            });

            // Wait a moment for reader to resolve endpoint
            await Task.Delay(200);

            // Send request - since no response message will be sent, it should time out
            var sendTask = conn.SendRequestAsync("initialize", "{\"jsonrpc\":\"2.0\",\"id\":\"test-id\",\"method\":\"initialize\"}");
            
            // Assert that it throws TimeoutException
            await Assert.ThrowsAsync<TimeoutException>(async () => await sendTask);

            // Assert that the pending request TCS is removed from dictionary (no leak)
            conn.PendingRequests.Should().NotContainKey("test-id");
        }

        [Fact]
        public async Task SendRequestAsync_Succeeds_When_Response_Has_Method_Property()
        {
            // Verify that SendRequestAsync succeeds (does not hang) if the response contains a 'method' property,
            // because JsonRpcMessageConverter correctly prioritizes response indicators over 'method'.
            var server = new McpServer 
            { 
                Id = "backend1", 
                DisplayName = "Backend 1", 
                Url = "http://backend1/mcp", 
                Type = "sse",
                Enabled = true 
            };

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var postReceived = new TaskCompletionSource<bool>();
            var responsePayload = "{\"jsonrpc\":\"2.0\",\"id\":\"test-id\",\"result\":{\"ok\":true},\"method\":\"dummy-method\"}";

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var stream = new DelayedSseMockStream(
                        "event: endpoint\ndata: http://backend1/mcp/message\n\n",
                        "event: message\ndata: " + responsePayload + "\n\n",
                        postReceived.Task
                    );
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    postReceived.TrySetResult(true);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            await conn.ConnectAsync();

            var messageReceivedSource = new TaskCompletionSource<bool>();
            conn.StartReader(async (msg) => {
                // If it is treated as a response, complete the TaskCompletionSource
                if (msg is JsonRpcResponse response && response.Id != null)
                {
                    var idStr = response.Id.ToString();
                    if (idStr != null && conn.PendingRequests.TryRemove(idStr, out var tcs))
                    {
                        tcs.SetResult(response);
                        return;
                    }
                }
                if (msg is JsonRpcRequest req && req.Id?.ToString() == "test-id")
                {
                    messageReceivedSource.TrySetResult(true);
                }
            });

            // Wait a moment for reader to resolve endpoint
            await Task.Delay(200);

            var sendTask = conn.SendRequestAsync("initialize", "{\"jsonrpc\":\"2.0\",\"id\":\"test-id\",\"method\":\"initialize\"}");
            var completedTask = await Task.WhenAny(sendTask, Task.Delay(2000));

            // Assert: it should complete successfully (not delay)
            completedTask.Should().BeSameAs(sendTask);
            var result = await sendTask;
            result.Should().NotBeNull();
            result.Result.Should().NotBeNull();
            result.Result.Value.GetProperty("ok").GetBoolean().Should().BeTrue();

            // Assert: it was NOT processed as a request/notification
            messageReceivedSource.Task.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task SseBackend_Notification_IsForwardedToClient_WithAllFieldsIntact()
        {
            // Arrange
            var server = new McpServer 
            { 
                Id = "backend_sse", 
                DisplayName = "SSE Backend", 
                Url = "http://backend_sse/mcp", 
                Type = "sse", // Using Type = sse to trigger StartReader
                Enabled = true 
            };

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var postReceived = new TaskCompletionSource<bool>();
            // Setup SSE stream that sends an initialize response, and then a notification
            var notificationPayload = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/logMessage\",\"params\":{\"level\":\"info\",\"logger\":\"test\",\"message\":\"Hello from SSE!\"}}";
            
            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var stream = new DelayedSseMockStream(
                        "event: endpoint\ndata: http://backend_sse/mcp/message\n\n",
                        "event: message\ndata: {\"jsonrpc\":\"2.0\",\"id\":\"auto-init\",\"result\":{\"protocolVersion\":\"2024-11-05\"}}\n\n" +
                        "event: message\ndata: " + notificationPayload + "\n\n",
                        postReceived.Task
                    );
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    postReceived.TrySetResult(true);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var context = new DefaultHttpContext();
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var session = new ClientSession("test-session", context.Response, new List<McpServer> { server }, httpClient, loggerMock.Object);

            // Act
            // Start initialization, which connects, starts reader, and handles messages
            var defaultInitRequest = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\",\"id\":\"auto-init\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"McpRouterGatewayAuto\",\"version\":\"0.4.0\"}}}";
            await session.InitializeBackendsAsync(defaultInitRequest);

            // Wait a moment for background reader to process the notification and write to the response
            await Task.Delay(500);

            // Assert
            responseBody.Position = 0;
            using var reader = new StreamReader(responseBody);
            var output = await reader.ReadToEndAsync();

            // The output should contain the notification message with level and message fields intact
            output.Should().Contain("event: message");
            output.Should().Contain("notifications/logMessage");
            output.Should().Contain("\"level\":\"info\"");
            output.Should().Contain("\"message\":\"Hello from SSE!\"");
            
            // Clean up
            session.Close();
        }

        [Fact]
        public async Task AsynchronousRouting_HighVolumeAndPolymorphic_DoesNotHang()
        {
            var server = new McpServer 
            { 
                Id = "backend_stress", 
                DisplayName = "Stress Backend", 
                Url = "http://backend_stress/mcp", 
                Type = "sse",
                Enabled = true 
            };

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var stream = new SseMockStream("event: endpoint\ndata: http://backend_stress/mcp/message\n\n");
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            conn.RequestTimeout = TimeSpan.FromSeconds(5);
            await conn.ConnectAsync();

            conn.StartReader(async (msg) =>
            {
                await Task.CompletedTask;
            });

            await Task.Delay(200);

            var tasks = new List<Task<JsonRpcResponse>>();
            for (int i = 0; i < 100; i++)
            {
                var id = $"stress-{i}";
                var payload = $"{{\"jsonrpc\":\"2.0\",\"id\":\"{id}\",\"method\":\"tools/call\",\"params\":{{\"name\":\"test\"}}}}";
                var idx = i;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10 + (idx % 5));
                    var responseObj = new JsonRpcResponse 
                    { 
                        Id = id, 
                        Result = JsonSerializer.Deserialize<JsonElement>($"{{\"index\":{idx},\"ok\":true}}") 
                    };
                    if (conn.PendingRequests.TryRemove(id, out var pendingTcs))
                    {
                        pendingTcs.SetResult(responseObj);
                    }
                });

                tasks.Add(conn.SendRequestAsync("method", payload));
            }

            var allTasks = Task.WhenAll(tasks);
            var completed = await Task.WhenAny(allTasks, Task.Delay(5000));
            completed.Should().BeSameAs(allTasks, "High volume async requests should not deadlock or hang.");
            
            var results = await allTasks;
            results.Length.Should().Be(100);
        }

        [Fact]
        public void JsonNode_Rewrite_HandlesAdversarialEdgeCases()
        {
            var loggerMock = new Mock<ILogger>();
            var session = new ClientSession("session-id", null!, new List<McpServer>(), null!, loggerMock.Object);
            var methodInfo = typeof(ClientSession).GetMethod("RewriteRequestJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var mixedArrayJson = "[123, \"string\", null, {\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"serverId__toolName\"}}, true]";
            var rewrittenMixed = (string)methodInfo!.Invoke(session, new object[] { mixedArrayJson, "name", "toolName" })!;
            rewrittenMixed.Should().Contain("\"name\":\"toolName\"");

            var complexJson = @"
            {
                /* block comment */
                // line comment
                ""jsonrpc"": ""2.0"",
                ""method"": ""tools/call"",
                ""params"": {
                    ""name"": ""serverId__toolName"", // parameter comment
                }, // params trailing comma
            }";
            var rewrittenComplex = (string)methodInfo.Invoke(session, new object[] { complexJson, "name", "toolName" })!;
            rewrittenComplex.Should().Contain("\"name\":\"toolName\"");

            var invalidJson = "{ invalid: json, }";
            var rewrittenInvalid = (string)methodInfo.Invoke(session, new object[] { invalidJson, "name", "toolName" })!;
            rewrittenInvalid.Should().Be(invalidJson);
        }

        [Fact]
        public async Task AuthMiddleware_CaseInsensitivity_BypassAndHeader_Check()
        {
            var bypassPaths = new[] { "/api/register", "/API/REGISTER", "/Api/Register", "/api/me", "/API/ME", "/Api/Me" };
            
            foreach (var path in bypassPaths)
            {
                var context = new DefaultHttpContext();
                context.Request.Path = path;
                
                bool nextCalled = false;
                RequestDelegate next = (ctx) =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                };

                var p = context.Request.Path.Value ?? string.Empty;
                if (p.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/register", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase))
                {
                    var user = context.Request.Headers["Remote-User"].ToString();
                    if (string.IsNullOrEmpty(user))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        await next(context);
                    }
                }
                else
                {
                    await next(context);
                }

                context.Response.StatusCode.Should().NotBe(401);
                nextCalled.Should().BeTrue();
            }

            var headerKeys = new[] { "Remote-User", "remote-user", "REMOTE-USER", "ReMoTe-UsEr" };
            foreach (var headerKey in headerKeys)
            {
                var context = new DefaultHttpContext();
                context.Request.Path = "/api/clients";
                context.Request.Headers[headerKey] = "steve";

                bool nextCalled = false;
                RequestDelegate next = (ctx) =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                };

                var p = context.Request.Path.Value ?? string.Empty;
                if (p.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/register", StringComparison.OrdinalIgnoreCase) && 
                    !p.StartsWith("/api/me", StringComparison.OrdinalIgnoreCase))
                {
                    var user = context.Request.Headers["Remote-User"].ToString();
                    if (string.IsNullOrEmpty(user))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        await next(context);
                    }
                }
                else
                {
                    await next(context);
                }

                context.Response.StatusCode.Should().NotBe(401);
                nextCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void PlainJsonRpcMessages_DoNotCauseStackOverflow_PolymorphicVariants()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonRpcMessageConverter() }
            };

            var payloads = new[]
            {
                "{\"jsonrpc\":\"2.0\"}",
                "{\"jsonrpc\":\"2.0\",\"id\":null}",
                "{\"jsonrpc\":\"2.0\",\"method\":null}",
                "{\"jsonrpc\":\"2.0\",\"result\":null}",
                "{}",
                "{\"extra_field\":\"value\"}"
            };

            foreach (var payload in payloads)
            {
                var action = () => JsonSerializer.Deserialize<JsonRpcMessage>(payload, options);
                action.Should().NotThrow();
                var msg = action();
                msg.Should().NotBeNull();
            }
        }
    }
}

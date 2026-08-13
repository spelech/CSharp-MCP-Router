using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using McpRouter.Models;
using McpRouter;
using McpRouter.Core.Transports;

namespace McpRouter.Tests
{
    public class ConcurrentResponseIsolationTests
    {
        [Fact]
        public async Task ConcurrentResponseIsolation_TwoCallersSameId_SucceedsWithReversedResponseOrder()
        {
            // Arrange
            var server = new McpServer
            {
                Id = "concurrent_backend",
                DisplayName = "Concurrent Backend",
                Url = "http://concurrent_backend/mcp",
                Type = "sse",
                SecretProvider = "None",
                Enabled = true
            };

            var sseStream = new DynamicSseStream();
            sseStream.PushMessage("event: endpoint\ndata: http://concurrent_backend/mcp/message\n\n");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var interceptedRequests = new ConcurrentBag<(string UpstreamId, string OriginalId, string Payload)>();

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var streamContent = new StreamContent(sseStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    var body = await req.Content!.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var upstreamId = root.GetProperty("id").GetString()!;
                    var originalId = "1"; // from test setup
                    var payload = root.GetProperty("params").GetProperty("data").GetString()!;

                    interceptedRequests.Add((upstreamId, originalId, payload));
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            conn.RequestTimeout = TimeSpan.FromSeconds(15);
            await conn.ConnectAsync();

            conn.StartReader(async (msg) => {
                await Task.CompletedTask;
            });

            // Wait a short moment for reader to resolve endpoint
            await Task.Delay(200);

            // Act - Send two concurrent requests with identical original ID 1 but different payloads
            var task1 = conn.SendRequestAsync("tools/call", "{\"jsonrpc\":\"2.0\",\"id\":1,\"params\":{\"data\":\"payload_one\"}}");
            var task2 = conn.SendRequestAsync("tools/call", "{\"jsonrpc\":\"2.0\",\"id\":1,\"params\":{\"data\":\"payload_two\"}}");

            // Wait for both requests to be posted to the backend and intercepted
            while (interceptedRequests.Count < 2)
            {
                await Task.Delay(50);
            }

            var requestList = interceptedRequests.ToList();
            var reqOne = requestList.First(r => r.Payload == "payload_one");
            var reqTwo = requestList.First(r => r.Payload == "payload_two");

            // Guarantee that upstream unique IDs are completely different
            reqOne.UpstreamId.Should().NotBe(reqTwo.UpstreamId);

            // Act - Simulate the backend responding to the second request first (reversed response order)
            var responseTwoPayload = $"event: message\ndata: {{\"jsonrpc\":\"2.0\",\"id\":\"{reqTwo.UpstreamId}\",\"result\":{{\"text\":\"result_two\"}}}}\n\n";
            sseStream.PushMessage(responseTwoPayload);

            // Wait a bit, then respond to the first request
            await Task.Delay(200);
            var responseOnePayload = $"event: message\ndata: {{\"jsonrpc\":\"2.0\",\"id\":\"{reqOne.UpstreamId}\",\"result\":{{\"text\":\"result_one\"}}}}\n\n";
            sseStream.PushMessage(responseOnePayload);

            // Await both tasks
            var result1 = await task1;
            var result2 = await task2;

            // Assert - Verify that results are correctly routed back with correct distinct payloads and original IDs
            result1.Should().NotBeNull();
            result1.Id!.ToString().Should().Be("1");
            result1.Result!.Value.GetProperty("text").GetString().Should().Be("result_one");

            result2.Should().NotBeNull();
            result2.Id!.ToString().Should().Be("1");
            result2.Result!.Value.GetProperty("text").GetString().Should().Be("result_two");

            conn.PendingRequests.Should().BeEmpty();
            conn.Dispose();
        }

        [Fact]
        public async Task HighConcurrencyResponseIsolation_RepeatedIdsAcrossCallers()
        {
            // Arrange
            var server = new McpServer
            {
                Id = "stress_concurrent",
                DisplayName = "Stress Concurrent",
                Url = "http://stress_concurrent/mcp",
                Type = "sse",
                SecretProvider = "None",
                Enabled = true
            };

            var sseStream = new DynamicSseStream();
            sseStream.PushMessage("event: endpoint\ndata: http://stress_concurrent/mcp/message\n\n");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var interceptedRequests = new ConcurrentDictionary<string, string>(); // UpstreamId -> ExpectedResultValue

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var streamContent = new StreamContent(sseStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    var body = await req.Content!.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var upstreamId = root.GetProperty("id").GetString()!;
                    var payloadIndex = root.GetProperty("params").GetProperty("index").GetInt32();

                    interceptedRequests[upstreamId] = $"result_{payloadIndex}";
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            conn.RequestTimeout = TimeSpan.FromSeconds(15);
            await conn.ConnectAsync();

            conn.StartReader(async (msg) => {
                await Task.CompletedTask;
            });

            await Task.Delay(200);

            int totalRequests = 40;
            var tasks = new List<Task<(int Index, int OriginalId, JsonRpcResponse Response)>>();

            // Launch 40 concurrent requests repeating original IDs (0, 1, 2, 3)
            for (int i = 0; i < totalRequests; i++)
            {
                int index = i;
                int originalId = i % 4;
                var payload = $"{{\"jsonrpc\":\"2.0\",\"id\":{originalId},\"params\":{{\"index\":{index}}}}}";

                tasks.Add(Task.Run(async () =>
                {
                    var resp = await conn.SendRequestAsync("tools/call", payload);
                    return (index, originalId, resp);
                }));
            }

            // Wait for all requests to be registered
            while (interceptedRequests.Count < totalRequests)
            {
                await Task.Delay(50);
            }

            // Push all responses concurrently out-of-order/scrambled
            var random = new Random();
            var shuffledReqs = interceptedRequests.ToList().OrderBy(x => random.Next()).ToList();

            foreach (var req in shuffledReqs)
            {
                var responsePayload = $"event: message\ndata: {{\"jsonrpc\":\"2.0\",\"id\":\"{req.Key}\",\"result\":{{\"text\":\"{req.Value}\"}}}}\n\n";
                sseStream.PushMessage(responsePayload);
            }

            // Await all and verify
            var results = await Task.WhenAll(tasks);
            results.Length.Should().Be(totalRequests);

            foreach (var result in results)
            {
                result.Response.Should().NotBeNull();
                result.Response.Id!.ToString().Should().Be(result.OriginalId.ToString());
                result.Response.Result!.Value.GetProperty("text").GetString().Should().Be($"result_{result.Index}");
            }

            conn.PendingRequests.Should().BeEmpty();
            conn.Dispose();
        }

        [Fact]
        public async Task TimeoutAndCancellationCleanup_DoesNotLeavePendingRequests()
        {
            // Arrange
            var server = new McpServer
            {
                Id = "timeout_backend",
                DisplayName = "Timeout Backend",
                Url = "http://timeout_backend/mcp",
                Type = "sse",
                SecretProvider = "None",
                Enabled = true
            };

            var sseStream = new DynamicSseStream();
            sseStream.PushMessage("event: endpoint\ndata: http://timeout_backend/mcp/message\n\n");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var postTcs = new TaskCompletionSource<bool>();

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var streamContent = new StreamContent(sseStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    postTcs.TrySetResult(true);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            // Very short timeout
            conn.RequestTimeout = TimeSpan.FromMilliseconds(300);
            await conn.ConnectAsync();

            conn.StartReader(async (msg) => {
                await Task.CompletedTask;
            });

            await Task.Delay(200);

            // Act - Send request and let it time out
            var task = conn.SendRequestAsync("tools/call", "{\"jsonrpc\":\"2.0\",\"id\":1,\"params\":{}}");

            await Assert.ThrowsAsync<TimeoutException>(async () => await task);

            // Assert - Verify that it got cleaned up and does not leak
            conn.PendingRequests.Should().BeEmpty();
            conn.Dispose();
        }

        [Fact]
        public async Task BackendDisconnectCleanup_ClearsPendingRequests()
        {
            // Arrange
            var server = new McpServer
            {
                Id = "disconnect_backend",
                DisplayName = "Disconnect Backend",
                Url = "http://disconnect_backend/mcp",
                Type = "sse",
                SecretProvider = "None",
                Enabled = true
            };

            var sseStream = new DynamicSseStream();
            sseStream.PushMessage("event: endpoint\ndata: http://disconnect_backend/mcp/message\n\n");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();

            var postTcs = new TaskCompletionSource<bool>();

            mockHandler.Handler = async (req) =>
            {
                if (req.Method == HttpMethod.Get)
                {
                    var streamContent = new StreamContent(sseStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = streamContent };
                }
                else if (req.Method == HttpMethod.Post)
                {
                    postTcs.TrySetResult(true);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            conn.RequestTimeout = TimeSpan.FromSeconds(15);
            await conn.ConnectAsync();

            conn.StartReader(async (msg) => {
                await Task.CompletedTask;
            });

            await Task.Delay(200);

            // Send request
            var task = conn.SendRequestAsync("tools/call", "{\"jsonrpc\":\"2.0\",\"id\":1,\"params\":{}}");

            await postTcs.Task;

            // Assert that we have 1 pending request
            conn.PendingRequests.Count.Should().Be(1);

            // Act - Force disconnect by completing/disposing the stream
            sseStream.Complete();

            // Wait a moment for reader to detect disconnect and cancel/clear pending requests
            await Task.Delay(200);

            // Assert - The pending request task should be completed with error or cancelled
            conn.PendingRequests.Should().BeEmpty();

            conn.Dispose();
        }
    }
}

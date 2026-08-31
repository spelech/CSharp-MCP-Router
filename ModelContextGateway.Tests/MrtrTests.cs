using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace ModelContextGateway.Tests
{
    public class MrtrTests
    {
        [Fact]
        [Requirement("MCP-MRTR-01", "MCP", RequirementType.Positive, "McpInputRequiredResult serializes and deserializes according to MCP 2026-07-28 spec with resultType input_required.")]
        public void Test_MrtrModels_Serialization()
        {
            var result = new McpInputRequiredResult
            {
                ResultType = "input_required",
                InputRequests = new List<McpInputRequest>
                {
                    new McpInputRequest
                    {
                        Id = "req_auth_code",
                        Type = "text",
                        Message = "Please enter 2FA verification code sent to your phone",
                        Required = true
                    }
                }
            };

            var json = JsonSerializer.Serialize(result);
            Assert.Contains("\"resultType\":\"input_required\"", json);
            Assert.Contains("\"inputRequests\":", json);
            Assert.Contains("\"id\":\"req_auth_code\"", json);

            var deserialized = JsonSerializer.Deserialize<McpInputRequiredResult>(json);
            Assert.NotNull(deserialized);
            Assert.Equal("input_required", deserialized.ResultType);
            Assert.Single(deserialized.InputRequests);
            Assert.Equal("req_auth_code", deserialized.InputRequests[0].Id);
            Assert.Equal("Please enter 2FA verification code sent to your phone", deserialized.InputRequests[0].Message);
        }

        [Fact]
        [Requirement("MCP-MRTR-02", "MCP", RequirementType.Positive, "execute_tool forwards inputResponses to target tool params during request retry.")]
        public async Task Test_ExecuteTool_ForwardsInputResponses()
        {
            var toolManager = new ToolRoutingManager();
            var servers = new List<McpServer>
            {
                new McpServer { Id = "testserver", DisplayName = "Test Server", Enabled = true, Type = "custom" }
            };

            toolManager.ToolRoutingTable.TryAdd("testserver__secure_tool", "testserver");

            var conn = new TestBackendConnection(async (method, bodyJson, token) =>
            {
                using var doc = JsonDocument.Parse(bodyJson);
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("params", out var paramsProp));
                Assert.True(paramsProp.TryGetProperty("inputResponses", out var inputRespProp));
                Assert.Equal("123456", inputRespProp.GetProperty("req_auth_code").GetString());

                return new JsonRpcResponse
                {
                    Id = "1",
                    Result = JsonSerializer.Deserialize<JsonElement>(
                        "{\"content\":[{\"type\":\"text\",\"text\":\"2FA Verification Successful\"}]}"
                    )
                };
            });

            var backendConnections = new ConcurrentDictionary<string, BackendConnection>();
            backendConnections.TryAdd("testserver", conn);

            var executeBody = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = "execute_tool",
                    arguments = new
                    {
                        name = "testserver__secure_tool",
                        arguments = new { action = "transfer" },
                        inputResponses = new
                        {
                            req_auth_code = "123456"
                        }
                    }
                }
            });

            var result = await toolManager.CallToolAsync(
                "execute_tool",
                executeBody,
                null!,
                backendConnections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                null!,
                () => Task.CompletedTask,
                (body, key, val) => body
            );

            Assert.NotNull(result);
            Assert.IsType<JsonRpcResponse>(result);
            var resp = (JsonRpcResponse)result;
            Assert.NotNull(resp.Result);
            Assert.Contains("2FA Verification Successful", resp.Result.Value.GetRawText());
        }

        [Fact]
        [Requirement("MCP-MRTR-03", "MCP", RequirementType.Positive, "Direct tool call returns InputRequiredResult intact when backend requires additional input.")]
        public async Task Test_DirectToolCall_ReturnsInputRequiredResult()
        {
            var toolManager = new ToolRoutingManager();
            var servers = new List<McpServer>
            {
                new McpServer { Id = "srv", DisplayName = "Target Server", Enabled = true, Type = "custom" }
            };

            toolManager.ToolRoutingTable.TryAdd("srv__prompt_user", "srv");

            var expectedResultJson = JsonSerializer.Serialize(new McpInputRequiredResult
            {
                ResultType = "input_required",
                InputRequests = new List<McpInputRequest>
                {
                    new McpInputRequest
                    {
                        Id = "prompt_confirm",
                        Type = "text",
                        Message = "Confirm action? (yes/no)"
                    }
                }
            });

            var conn = new TestBackendConnection(async (method, bodyJson, token) =>
            {
                return new JsonRpcResponse
                {
                    Id = "10",
                    Result = JsonSerializer.Deserialize<JsonElement>(expectedResultJson)
                };
            });

            var backendConnections = new ConcurrentDictionary<string, BackendConnection>();
            backendConnections.TryAdd("srv", conn);

            var requestBody = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 10,
                method = "tools/call",
                @params = new
                {
                    name = "srv__prompt_user",
                    arguments = new { target = "account" }
                }
            });

            var result = await toolManager.CallToolAsync(
                "srv__prompt_user",
                requestBody,
                null!,
                backendConnections,
                servers,
                NullLogger.Instance,
                new HttpClient(),
                null!,
                () => Task.CompletedTask,
                (body, key, val) => body
            );

            Assert.NotNull(result);
            Assert.IsType<JsonRpcResponse>(result);
            var resp = (JsonRpcResponse)result;
            Assert.NotNull(resp.Result);
            var resultStr = resp.Result.Value.GetRawText();
            Assert.Contains("\"resultType\":\"input_required\"", resultStr);
            Assert.Contains("prompt_confirm", resultStr);
        }

        private class TestBackendConnection : BackendConnection
        {
            private readonly Func<string, string, string?, Task<JsonRpcResponse>> _handler;

            public TestBackendConnection(Func<string, string, string?, Task<JsonRpcResponse>> handler)
                : base(new McpServer { Id = "test", Type = "custom" }, new HttpClient(), NullLogger.Instance)
            {
                _handler = handler;
            }

            public override Task<JsonRpcResponse> SendRequestAsync(string method, string bodyJson, string? targetAuthToken = null)
            {
                return _handler(method, bodyJson, targetAuthToken);
            }
        }
    }
}

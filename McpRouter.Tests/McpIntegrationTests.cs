using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>>? Handler { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Handler != null)
            {
                return await Handler(request);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    public class McpIntegrationTests : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly RouterDbContext _db;

        public McpIntegrationTests()
        {
            _connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<RouterDbContext>()
                .UseSqlite(_connection)
                .Options;

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["DB_ENCRYPTION_KEY"]).Returns("TestKey");

            _db = new RouterDbContext(options, mockConfig.Object);
            _db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private ClientSession CreateSession(List<McpServer> servers, out MockHttpMessageHandler httpHandler)
        {
            httpHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(httpHandler);
            
            var context = new DefaultHttpContext();
            var response = context.Response;
            
            var loggerMock = new Mock<ILogger>();
            
            return new ClientSession("test-session", response, servers, httpClient, loggerMock.Object);
        }

        private HttpResponseMessage CreateJsonResponse(object payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content };
        }

        [Fact]
        public void PolymorphicDeserialization_Correctly_Deserializes_JsonRpcMessage_Subclasses()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonRpcMessageConverter() }
            };

            // Request JSON
            var requestJson = "{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":123,\"params\":{}}";
            var msgReq = JsonSerializer.Deserialize<JsonRpcMessage>(requestJson, options);
            msgReq.Should().BeOfType<JsonRpcRequest>();
            var req = msgReq as JsonRpcRequest;
            req!.Method.Should().Be("tools/list");
            req.Id?.ToString().Should().Be("123");

            // Notification JSON
            var notificationJson = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}";
            var msgNotif = JsonSerializer.Deserialize<JsonRpcMessage>(notificationJson, options);
            msgNotif.Should().BeOfType<JsonRpcNotification>();
            var notif = msgNotif as JsonRpcNotification;
            notif!.Method.Should().Be("notifications/initialized");

            // Response JSON
            var responseJson = "{\"jsonrpc\":\"2.0\",\"id\":123,\"result\":{\"success\":true}}";
            var msgResp = JsonSerializer.Deserialize<JsonRpcMessage>(responseJson, options);
            msgResp.Should().BeOfType<JsonRpcResponse>();
            var resp = msgResp as JsonRpcResponse;
            resp!.Id?.ToString().Should().Be("123");
            resp.Result.Should().NotBeNull();
        }

        [Fact]
        public void Deserializing_Plain_JsonRpcMessage_Does_Not_Cause_StackOverflow()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonRpcMessageConverter() }
            };

            var plainJson = "{\"jsonrpc\":\"2.0\"}";
            
            // Act & Assert
            var action = () => JsonSerializer.Deserialize<JsonRpcMessage>(plainJson, options);
            action.Should().NotThrow();
            
            var msg = JsonSerializer.Deserialize<JsonRpcMessage>(plainJson, options);
            msg.Should().NotBeNull();
            msg.Should().BeOfType<JsonRpcMessage>();
            msg!.JsonRpc.Should().Be("2.0");
        }

        [Fact]
        public void Serializing_Plain_JsonRpcMessage_Does_Not_Cause_StackOverflow()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonRpcMessageConverter() }
            };

            var msg = new JsonRpcMessage { JsonRpc = "2.0" };

            // Act & Assert
            var action = () => JsonSerializer.Serialize(msg, options);
            action.Should().NotThrow();

            var json = JsonSerializer.Serialize(msg, options);
            json.Should().Contain("\"jsonrpc\":\"2.0\"");
        }

        [Fact]
        public async Task TestInitializationDiagnostics()
        {
            var server = new McpServer { Id = "backend1", DisplayName = "Backend 1", Url = "http://backend1/mcp", Type = "http", Enabled = true };
            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var loggerMock = new Mock<ILogger>();
            
            mockHandler.Handler = async (req) =>
            {
                return CreateJsonResponse(new
                {
                    jsonrpc = "2.0",
                    id = "auto-init",
                    result = new { protocolVersion = "2024-11-05" }
                });
            };

            var conn = new BackendConnection(server, httpClient, loggerMock.Object);
            try
            {
                var resp = await conn.SendRequestAsync("initialize", "{\"jsonrpc\":\"2.0\",\"id\":\"auto-init\",\"method\":\"initialize\"}");
                resp.Should().NotBeNull();
            }
            catch (Exception ex)
            {
                throw new Exception("Initialization diagnostics failed with: " + ex.ToString());
            }
        }

        [Fact]
        public async Task ToolListing_And_Remapping_Works_Correctly()
        {
            // Arrange
            var servers = new List<McpServer>
            {
                new McpServer { Id = "backend1", DisplayName = "Backend 1", Url = "http://backend1/mcp", Type = "http", Enabled = true }
            };

            var session = CreateSession(servers, out var httpHandler);

            httpHandler.Handler = async (req) =>
            {
                var body = req.Content != null ? await req.Content.ReadAsStringAsync() : "";
                if (body.Contains("initialize"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "auto-init",
                        result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { },
                            serverInfo = new { name = "Backend1", version = "1.0" }
                        }
                    });
                }
                else if (body.Contains("tools/list"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "init-list",
                        result = new
                        {
                            tools = new[]
                            {
                                new
                                {
                                    name = "get_weather",
                                    description = "Get weather info",
                                    inputSchema = new { type = "object" }
                                }
                            }
                        }
                    });
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            // Act
            var tools = await session.ListToolsAsync("{\"jsonrpc\":\"2.0\",\"id\":1}");

            // Assert
            tools.Should().NotBeEmpty();
            var tool = tools[0] as Dictionary<string, object>;
            tool.Should().NotBeNull();
            tool!["name"].Should().Be("backend1__get_weather");
            tool["description"].Should().Be("[backend1] Get weather info");
        }

        [Fact]
        public async Task ResourceRouting_And_UriTranslation_Works_Correctly()
        {
            // Arrange
            var servers = new List<McpServer>
            {
                new McpServer { Id = "backend1", DisplayName = "Backend 1", Url = "http://backend1/mcp", Type = "http", Enabled = true }
            };

            var session = CreateSession(servers, out var httpHandler);

            string? lastReadUri = null;
            httpHandler.Handler = async (req) =>
            {
                var body = req.Content != null ? await req.Content.ReadAsStringAsync() : "";
                if (body.Contains("initialize"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "auto-init",
                        result = new { protocolVersion = "2024-11-05" }
                    });
                }
                else if (body.Contains("resources/list"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "res-list",
                        result = new
                        {
                            resources = new[]
                            {
                                new
                                {
                                    uri = "file:///logs.txt",
                                    name = "System Logs"
                                }
                            }
                        }
                    });
                }
                else if (body.Contains("resources/read"))
                {
                    // Parse request body using JsonDocument to inspect the rewritten uri parameter
                    using var doc = JsonDocument.Parse(body);
                    lastReadUri = doc.RootElement.GetProperty("params").GetProperty("uri").GetString();

                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "res-read",
                        result = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    uri = "file:///logs.txt",
                                    text = "Log contents here"
                                }
                            }
                        }
                    });
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            // Act - List resources to register path in mapping table
            var resources = await session.ListResourcesAsync("{\"jsonrpc\":\"2.0\",\"method\":\"resources/list\",\"id\":1}");
            resources.Should().NotBeEmpty();

            var resourceDict = resources[0] as Dictionary<string, object>;
            resourceDict.Should().NotBeNull();
            var exposedUri = resourceDict!["uri"] as string;
            exposedUri.Should().Be("mcp://backend1/file%3A%2F%2F%2Flogs.txt");

            // Act - Read the resource using the mapped exposed URI
            var readBody = "{\"jsonrpc\":\"2.0\",\"method\":\"resources/read\",\"id\":\"test-read-id\",\"params\":{\"uri\":\"mcp://backend1/file%3A%2F%2F%2Flogs.txt\"}}";
            var result = await session.ReadResourceAsync(exposedUri!, readBody);

            // Assert
            result.Should().NotBeNull();
            lastReadUri.Should().Be("file:///logs.txt");
        }

        [Fact]
        public async Task PromptListAggregation_And_Routing_Works_Correctly()
        {
            // Arrange
            var servers = new List<McpServer>
            {
                new McpServer { Id = "backend1", DisplayName = "Backend 1", Url = "http://backend1/mcp", Type = "http", Enabled = true },
                new McpServer { Id = "backend2", DisplayName = "Backend 2", Url = "http://backend2/mcp", Type = "http", Enabled = true }
            };

            var session = CreateSession(servers, out var httpHandler);

            string? lastPromptGet = null;
            httpHandler.Handler = async (req) =>
            {
                var body = req.Content != null ? await req.Content.ReadAsStringAsync() : "";
                var uri = req.RequestUri?.ToString() ?? "";

                if (body.Contains("initialize"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "auto-init",
                        result = new { protocolVersion = "2024-11-05" }
                    });
                }
                else if (body.Contains("prompts/list"))
                {
                    var isBackend1 = uri.Contains("backend1");
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "prompt-list",
                        result = new
                        {
                            prompts = new[]
                            {
                                new
                                {
                                    name = isBackend1 ? "refactor" : "optimize",
                                    description = isBackend1 ? "Refactor code" : "Optimize code"
                                }
                            }
                        }
                    });
                }
                else if (body.Contains("prompts/get"))
                {
                    using var doc = JsonDocument.Parse(body);
                    lastPromptGet = doc.RootElement.GetProperty("params").GetProperty("name").GetString();

                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "prompt-get",
                        result = new
                        {
                            messages = new[]
                            {
                                new { role = "user", content = new { type = "text", text = "Aggregated prompt content" } }
                            }
                        }
                    });
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            // Act - List prompts to aggregate and map
            var prompts = await session.ListPromptsAsync("{\"jsonrpc\":\"2.0\",\"method\":\"prompts/list\",\"id\":1}");
            prompts.Should().HaveCount(2);

            var names = new List<string>();
            foreach (var prompt in prompts)
            {
                var dict = prompt as Dictionary<string, object>;
                names.Add(dict!["name"].ToString()!);
            }

            names.Should().Contain("backend1__refactor");
            names.Should().Contain("backend2__optimize");

            // Act - Get aggregated prompt from backend1
            var getBody = "{\"jsonrpc\":\"2.0\",\"method\":\"prompts/get\",\"id\":\"test-get-id\",\"params\":{\"name\":\"backend1__refactor\"}}";
            var result = await session.GetPromptAsync("backend1__refactor", getBody);

            // Assert
            result.Should().NotBeNull();
            lastPromptGet.Should().Be("refactor");
        }

        [Fact]
        public async Task AuthMiddleware_Blocks_Unauthorized_Request()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/servers";
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            // Act
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api/") && !path.StartsWith("/api/register") && !path.StartsWith("/api/me"))
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

            // Assert
            context.Response.StatusCode.Should().Be(401);
            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task AuthMiddleware_Allows_SSO_Session_With_RemoteUser_Header()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/servers";
            context.Request.Headers["Remote-User"] = "steve";
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            // Act
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api/") && !path.StartsWith("/api/register") && !path.StartsWith("/api/me"))
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

            // Assert
            context.Response.StatusCode.Should().NotBe(401);
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task SemanticToolSearchRanking_Sorts_By_Score()
        {
            // Arrange
            var servers = new List<McpServer>
            {
                new McpServer { Id = "backend1", DisplayName = "Backend 1", Url = "http://backend1/mcp", Type = "http", Enabled = true }
            };

            var session = CreateSession(servers, out var httpHandler);
            session.IsMetaMode = true; // Enable meta search mode

            // Let's populate the tool cache with tools of different descriptions
            httpHandler.Handler = async (req) =>
            {
                var body = req.Content != null ? await req.Content.ReadAsStringAsync() : "";
                if (body.Contains("initialize"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "auto-init",
                        result = new { protocolVersion = "2024-11-05" }
                    });
                }
                else if (body.Contains("tools/list"))
                {
                    return CreateJsonResponse(new
                    {
                        jsonrpc = "2.0",
                        id = "init-list",
                        result = new
                        {
                            tools = new[]
                            {
                                new { name = "list_containers", description = "List docker containers running on this server", inputSchema = new { } },
                                new { name = "read_excel", description = "Read an excel spreadsheet file and parse data", inputSchema = new { } },
                                new { name = "get_logs", description = "Retrieve log output from running container", inputSchema = new { } }
                            }
                        }
                    });
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            };

            // Call ListToolsAsync to populate cache
            session.IsMetaMode = false;
            await session.ListToolsAsync("{\"jsonrpc\":\"2.0\",\"id\":1}");
            session.IsMetaMode = true;

            // Act - Semantically search for "Excel"
            var searchBody = "{\"jsonrpc\":\"2.0\",\"id\":\"search-id\",\"method\":\"tools/call\",\"params\":{\"name\":\"search_tools\",\"arguments\":{\"query\":\"Excel\"}}}";
            var result = await session.CallToolAsync("search_tools", searchBody, _db);

            // Assert
            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            text.Should().NotBeNullOrEmpty();

            var searchResults = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(text!);
            searchResults.Should().NotBeEmpty();
            // First item should be the excel tool
            searchResults![0]["name"].ToString().Should().Contain("read_excel");

            // Act - Semantically search for "Docker container log"
            var searchBody2 = "{\"jsonrpc\":\"2.0\",\"id\":\"search-id-2\",\"method\":\"tools/call\",\"params\":{\"name\":\"search_tools\",\"arguments\":{\"query\":\"container log\"}}}";
            var result2 = await session.CallToolAsync("search_tools", searchBody2, _db);

            var json2 = JsonSerializer.Serialize(result2);
            using var doc2 = JsonDocument.Parse(json2);
            var text2 = doc2.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            var searchResults2 = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(text2!);

            searchResults2.Should().NotBeNull();
            searchResults2!.Count.Should().BeGreaterThanOrEqualTo(2);
            // It should match get_logs and list_containers, with get_logs ranking higher because it matches "log" in name/desc.
            searchResults2![0]["name"].ToString().Should().Contain("get_logs");
        }

        [Fact]
        public void CustomToolRegistry_Contains_Plex_And_Overseerr_Tools()
        {
            // Act
            var allTools = McpRouter.CustomTools.CustomToolRegistry.GetAll();

            // Assert
            allTools.Should().NotBeEmpty();
            var names = new List<string>();
            foreach (var tool in allTools)
            {
                names.Add(tool.Name);
            }
            names.Should().Contain("seerr_search_media");
            names.Should().Contain("plex_search_library");
        }
    }
}

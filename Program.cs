using Microsoft.AspNetCore.Builder;
using McpRouter.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddMcpRouterServices();

var app = builder.Build();

app.ConfigureMcpRouterPipeline();

app.Run();

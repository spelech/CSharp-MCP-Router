var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["MCG_PORT"] ?? builder.Configuration["PORT"] ?? Environment.GetEnvironmentVariable("MCG_PORT") ?? Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

builder.AddMcpRouterServices();

var app = builder.Build();

app.ConfigureMcpRouterPipeline();

app.Run();

public partial class Program { }

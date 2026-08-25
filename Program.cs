var builder = WebApplication.CreateBuilder(args);

builder.AddMcpRouterServices();

var app = builder.Build();

app.ConfigureMcpRouterPipeline();

app.Run();

public partial class Program { }

import os
import re

with open('/containers/mcp/router/Program.cs', 'r') as f:
    content = f.read()

# Extract DI
di_code = """
        // Add logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Register SQLite Database
        builder.Services.AddDbContext<RouterDbContext>();

        // Register OpenIddict & Controllers
        builder.Services.AddMcpOpenIddict();
        builder.Services.AddControllers();

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<SessionManager>();

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
"""

service_ext_code = f"""using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter.Extensions
{{
    public static class ServiceCollectionExtensions
    {{
        public static void AddMcpRouterServices(this WebApplicationBuilder builder)
        {{
{di_code}
        }}
    }}
}}
"""

with open('/containers/mcp/router/Extensions/ServiceCollectionExtensions.cs', 'w') as f:
    f.write(service_ext_code)

# Extract pipeline
# find everything from app.UseCors(); to the end of the file
pipeline_match = re.search(r'(app\.UseCors\(\);.*)', content, re.DOTALL)
if pipeline_match:
    pipeline_code = pipeline_match.group(1)
    # the pipeline uses Results.Ok, sessionManager, db, logger etc.
    # it needs to be inside an extension method
    pipeline_code = pipeline_code.replace("app.", "app.") # no change needed for app. methods
    
    app_ext_code = f"""using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpRouter.Models;
using McpRouter.Services;

namespace McpRouter.Extensions
{{
    public static class ApplicationBuilderExtensions
    {{
        public static void ConfigureMcpRouterPipeline(this WebApplication app)
        {{
            {pipeline_code.replace(chr(10), chr(10) + '            ')}
        }}
    }}
}}
"""
    with open('/containers/mcp/router/Extensions/ApplicationBuilderExtensions.cs', 'w') as f:
        f.write(app_ext_code)

new_program_code = """using Microsoft.AspNetCore.Builder;
using McpRouter.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddMcpRouterServices();

var app = builder.Build();

app.ConfigureMcpRouterPipeline();

app.Run();
"""
with open('/containers/mcp/router/Program.cs', 'w') as f:
    f.write(new_program_code)

import re

path = 'Extensions/ServiceCollectionExtensions.cs'
with open(path, 'r', newline='') as f:
    content = f.read()

target = "builder.Services.AddSingleton<CompositeSecretRetriever>();"
replacement = "builder.Services.AddSingleton<CompositeSecretRetriever>();\n            builder.Services.AddSingleton<McpRouter.Infrastructure.Secrets.IUserSecretStore, McpRouter.Infrastructure.Secrets.DatabaseUserSecretStore>();"

if target in content:
    content = content.replace(target, replacement)
    with open(path, 'w', newline='') as f:
        f.write(content)
    print("Success")
else:
    print("Target not found")

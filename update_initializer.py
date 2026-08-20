import os
path = '/home/steve/.gemini/antigravity-cli/brain/417528b4-d39f-4b8a-aad8-131d838d2ab8/.system_generated/worktrees/subagent-Backend-Routing-Engineer-routing-eng-23b3fe4a/Core/Routing/ClientSession/ClientSession.BackendInitializer.cs'
with open(path, 'r') as f: content = f.read()

target = """                    var retriever = _rootServices?.GetService<CompositeSecretRetriever>()
                        ?? _clientResponse?.HttpContext?.RequestServices?.GetService<CompositeSecretRetriever>();
                                        string? passThroughToken = null;
                    if (server.AllowPassThroughAuth && _clientResponse?.HttpContext != null)
                    {
                        if (_clientResponse.HttpContext.Request.Headers.TryGetValue("X-Target-Auth", out var tokenVals))
                        {
                            passThroughToken = tokenVals.ToString();
                        }
                    }"""

replacement = """                    var retriever = _rootServices?.GetService<CompositeSecretRetriever>()
                        ?? _clientResponse?.HttpContext?.RequestServices?.GetService<CompositeSecretRetriever>();
                    string? passThroughToken = null;
                    if (server.SecretProvider == "UserProvided")
                    {
                        var identity = await ResolveUserIdentityAsync(_clientResponse?.HttpContext);
                        var userSecretStore = _rootServices?.GetService<IUserSecretStore>()
                            ?? _clientResponse?.HttpContext?.RequestServices?.GetService<IUserSecretStore>();
                        if (userSecretStore != null)
                        {
                            var secretJson = await userSecretStore.GetSecretAsync(identity.Username, server.Id);
                            if (string.IsNullOrEmpty(secretJson))
                                throw new Exception($"User credential required but not found for server '{server.Id}'");
                            passThroughToken = secretJson;
                        }
                        else
                        {
                            throw new Exception("IUserSecretStore is not registered in DI.");
                        }
                    }
                    else if (server.AllowPassThroughAuth && _clientResponse?.HttpContext != null)
                    {
                        if (_clientResponse.HttpContext.Request.Headers.TryGetValue("X-Target-Auth", out var tokenVals))
                        {
                            passThroughToken = tokenVals.ToString();
                        }
                    }"""

if target in content:
    content = content.replace(target, replacement)
    with open(path, 'w') as f: f.write(content)
    print("Success")
else:
    print("Target not found")

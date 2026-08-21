const fs = require('fs');
let content = fs.readFileSync('Core/Routing/ToolRoutingManager.cs', 'utf8');

content = content.replace(
    'arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." }',
    'arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." },\n                            target_auth_token = new { type = "string", description = "Optional authentication token if the backend tool requires dynamic pass-through authorization." }'
);

content = content.replace(
    'if (toolDict.TryGetValue("description", out var desc))\n                                    toolDict["description"] = $"[{item.ServerId}] " + desc;\n                                serverTools.Add(toolDict);',
    'if (toolDict.TryGetValue("description", out var desc))\n                                {\n                                    toolDict["description"] = $"[{item.ServerId}] " + desc;\n                                }\n\n                                var srv = servers.FirstOrDefault(s => s.Id == item.ServerId);\n                                if (srv != null && (srv.AllowPassThroughAuth || !string.IsNullOrEmpty(srv.DynamicAuthPrompt)))\n                                {\n                                    var authPrompt = !string.IsNullOrEmpty(srv.DynamicAuthPrompt) ? srv.DynamicAuthPrompt : "This tool requires a target authentication token. Call with target_auth_token parameter.";\n                                    toolDict["description"] = $"{toolDict["description"]}\\n\\nAUTH REQUIRED: {authPrompt}";\n                                }\n\n                                serverTools.Add(toolDict);'
);

content = content.replace(
    'JsonElement targetArgs = default;\n\n                if (root.TryGetProperty("params", out var paramsProp) &&\n                    paramsProp.TryGetProperty("arguments", out var argsProp))\n                {\n                    if (argsProp.TryGetProperty("name", out var nameProp))\n                    {\n                        targetName = nameProp.GetString() ?? "";\n                    }\n                    if (argsProp.TryGetProperty("arguments", out var targetArgsProp))\n                    {\n                        targetArgs = targetArgsProp.Clone();\n                    }\n                }',
    'JsonElement targetArgs = default;\n                string? targetAuthToken = null;\n\n                if (root.TryGetProperty("params", out var paramsProp) &&\n                    paramsProp.TryGetProperty("arguments", out var argsProp))\n                {\n                    if (argsProp.TryGetProperty("name", out var nameProp))\n                    {\n                        targetName = nameProp.GetString() ?? "";\n                    }\n                    if (argsProp.TryGetProperty("arguments", out var targetArgsProp))\n                    {\n                        targetArgs = targetArgsProp.Clone();\n                    }\n                    if (argsProp.TryGetProperty("target_auth_token", out var targetAuthTokenProp))\n                    {\n                        targetAuthToken = targetAuthTokenProp.GetString();\n                    }\n                }'
);

content = content.replace(
    'var result = await ExecuteTargetToolAsync(targetName, targetBody, dbFactory, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, cancellationToken, sessionManager, clientSessionId);',
    'var result = await ExecuteTargetToolAsync(targetName, targetBody, targetAuthToken, dbFactory, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, cancellationToken, sessionManager, clientSessionId);'
);

content = content.replace(
    'return await ExecuteTargetToolAsync(toolName, body, dbFactory, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, cancellationToken, sessionManager, clientSessionId);',
    'return await ExecuteTargetToolAsync(toolName, body, null, dbFactory, backendConnections, servers, logger, httpClient, ensureBackendsInitializedAsync, rewriteRequestJson, cancellationToken, sessionManager, clientSessionId);'
);

content = content.replace(
    'private async Task<object> ExecuteTargetToolAsync(\n            string toolName,\n            string body,\n            IDbConnectionFactory dbFactory,',
    'private async Task<object> ExecuteTargetToolAsync(\n            string toolName,\n            string body,\n            string? targetAuthToken,\n            IDbConnectionFactory dbFactory,'
);

content = content.replace(
    'var resp = await conn.SendRequestAsync("tools/call", routingBody);',
    'var resp = await conn.SendRequestAsync("tools/call", routingBody, targetAuthToken);'
);

content = content.replace(
    'catch (Exception ex)\n                {\n                    var transformed = ToolErrorFormatter.TransformException(ex, toolName, serverId);',
    'catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)\n                {\n                    var srv = servers.FirstOrDefault(s => s.Id == serverId);\n                    var prompt = (srv != null && !string.IsNullOrEmpty(srv.DynamicAuthPrompt)) ? srv.DynamicAuthPrompt : "401 Unauthorized. Please provide a valid target_auth_token via execute_tool.";\n                    return new\n                    {\n                        isError = true,\n                        content = new[] {\n                            new {\n                                type = "text",\n                                text = prompt\n                            }\n                        }\n                    };\n                }\n                catch (Exception ex)\n                {\n                    var transformed = ToolErrorFormatter.TransformException(ex, toolName, serverId);'
);

fs.writeFileSync('Core/Routing/ToolRoutingManager.cs', content);

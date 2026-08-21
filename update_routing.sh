#!/bin/bash
sed -i 's/arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." }/arguments = new { type = "object", description = "The arguments JSON object expected by the target tool." },\n                            target_auth_token = new { type = "string", description = "Optional authentication token if the backend tool requires dynamic pass-through authorization." }/g' Core/Routing/ToolRoutingManager.cs

# Update PopulateToolsCacheAsync
sed -i 's/if (toolDict.TryGetValue("description", out var desc))/if (toolDict.TryGetValue("description", out var desc))\n                                {\n                                    toolDict["description"] = $"[{item.ServerId}] " + desc;\n                                }\n\n                                var srv = servers.FirstOrDefault(s => s.Id == item.ServerId);\n                                if (srv != null \&\& (srv.AllowPassThroughAuth || !string.IsNullOrEmpty(srv.DynamicAuthPrompt)))\n                                {\n                                    var authPrompt = !string.IsNullOrEmpty(srv.DynamicAuthPrompt) ? srv.DynamicAuthPrompt : "This tool requires a target authentication token. Call with target_auth_token parameter.";\n                                    toolDict["description"] = $"{toolDict["description"]}\\n\\nAUTH REQUIRED: {authPrompt}";\n                                }/g' Core/Routing/ToolRoutingManager.cs
sed -i 's/toolDict\["description"\] = $"\[{item.ServerId}\] " + desc;//g' Core/Routing/ToolRoutingManager.cs

# Fix execute_tool extraction
sed -i 's/JsonElement targetArgs = default;/JsonElement targetArgs = default;\n                string? targetAuthToken = null;/g' Core/Routing/ToolRoutingManager.cs

sed -i '/if (argsProp.TryGetProperty("arguments", out var targetArgsProp))/,+3a \                    if (argsProp.TryGetProperty("target_auth_token", out var targetAuthTokenProp))\n                    {\n                        targetAuthToken = targetAuthTokenProp.GetString();\n                    }' Core/Routing/ToolRoutingManager.cs

# Fix ExecuteTargetToolAsync calls and signature
sed -i 's/var result = await ExecuteTargetToolAsync(targetName, targetBody, dbFactory/var result = await ExecuteTargetToolAsync(targetName, targetBody, targetAuthToken, dbFactory/g' Core/Routing/ToolRoutingManager.cs
sed -i 's/return await ExecuteTargetToolAsync(toolName, body, dbFactory/return await ExecuteTargetToolAsync(toolName, body, null, dbFactory/g' Core/Routing/ToolRoutingManager.cs

sed -i '/private async Task<object> ExecuteTargetToolAsync(/a \            string? targetAuthToken,' Core/Routing/ToolRoutingManager.cs

sed -i 's/var resp = await conn.SendRequestAsync("tools\/call", routingBody);/var resp = await conn.SendRequestAsync("tools\/call", routingBody, targetAuthToken);/g' Core/Routing/ToolRoutingManager.cs


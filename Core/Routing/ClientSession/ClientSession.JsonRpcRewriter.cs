using System.Text.Json;

namespace ModelContextGateway.Core.Routing
{
    public partial class ClientSession
    {
        private string RewriteRequestJson(string body, string paramKey, string newValue)
        {
            try
            {
                var docOptions = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };
                var node = System.Text.Json.Nodes.JsonNode.Parse(body, null, docOptions);
                if (node == null)
                {
                    return body;
                }

                if (node is System.Text.Json.Nodes.JsonObject obj)
                {
                    RewriteObject(obj, paramKey, newValue);
                }
                else if (node is System.Text.Json.Nodes.JsonArray array)
                {
                    foreach (var item in array)
                    {
                        if (item is System.Text.Json.Nodes.JsonObject itemObj)
                        {
                            RewriteObject(itemObj, paramKey, newValue);
                        }
                    }
                }
                return node.ToJsonString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse and rewrite JSON body for key '{ParamKey}' to '{NewValue}'", paramKey, newValue);
                return body;
            }
        }

        private static void RewriteObject(System.Text.Json.Nodes.JsonObject obj, string paramKey, string newValue)
        {
            if (obj.TryGetPropertyValue("params", out var paramsNode) && paramsNode is System.Text.Json.Nodes.JsonObject paramsObj)
            {
                paramsObj[paramKey] = newValue;
            }

            var currentActivity = System.Diagnostics.Activity.Current;
            if (currentActivity != null && !string.IsNullOrEmpty(currentActivity.Id))
            {
                if (!obj.TryGetPropertyValue("_meta", out var metaNode) || metaNode is not System.Text.Json.Nodes.JsonObject)
                {
                    var metaObj = new System.Text.Json.Nodes.JsonObject();
                    metaObj["traceparent"] = currentActivity.Id;
                    if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
                    {
                        metaObj["tracestate"] = currentActivity.TraceStateString;
                    }
                    obj["_meta"] = metaObj;
                }
                else if (metaNode is System.Text.Json.Nodes.JsonObject existingMeta)
                {
                    if (existingMeta["traceparent"] == null)
                    {
                        existingMeta["traceparent"] = currentActivity.Id;
                    }
                    if (existingMeta["tracestate"] == null && !string.IsNullOrEmpty(currentActivity.TraceStateString))
                    {
                        existingMeta["tracestate"] = currentActivity.TraceStateString;
                    }
                }
            }
        }
    }
}

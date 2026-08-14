using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using McpRouter.Controllers;

namespace McpRouter.Core.Security
{
    public static class ProviderConfigSecurityHelper
    {
        public const string MaskValue = "********";

        private static readonly HashSet<string> SensitiveKeyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "token",
            "vault_token",
            "vaulttoken",
            "secretid",
            "secret_id",
            "roleid",
            "role_id",
            "secret",
            "clientsecret",
            "client_secret",
            "password",
            "bindpassword",
            "bind_password",
            "apikey",
            "api_key",
            "serviceaccountpassword",
            "service_account_password",
            "masterkey",
            "master_key"
        };

        public static bool IsSensitiveKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return false;
            if (SensitiveKeyNames.Contains(keyName)) return true;
            var lower = keyName.ToLowerInvariant();
            return lower.Contains("secret") || lower.Contains("token") || lower.Contains("password") || lower.Contains("apikey");
        }

        public static string? RedactConfigJson(string? configJson)
        {
            if (string.IsNullOrWhiteSpace(configJson)) return configJson;
            try
            {
                var node = JsonNode.Parse(configJson);
                if (node == null) return configJson;
                RedactJsonNode(node);
                return node.ToJsonString();
            }
            catch
            {
                return configJson;
            }
        }

        private static void RedactJsonNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var properties = obj.ToList();
                foreach (var (propName, propVal) in properties)
                {
                    if (propVal == null) continue;
                    if (IsSensitiveKey(propName))
                    {
                        if (propVal is JsonValue val && val.TryGetValue<string>(out var strVal))
                        {
                            if (!string.IsNullOrEmpty(strVal))
                            {
                                obj[propName] = MaskValue;
                            }
                        }
                    }
                    else
                    {
                        RedactJsonNode(propVal);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item != null) RedactJsonNode(item);
                }
            }
        }

        public static string? MergeWithExistingConfig(string? incomingConfigJson, string? existingDecryptedConfigJson)
        {
            if (string.IsNullOrWhiteSpace(incomingConfigJson)) return incomingConfigJson;
            if (string.IsNullOrWhiteSpace(existingDecryptedConfigJson)) return incomingConfigJson;

            try
            {
                var incomingNode = JsonNode.Parse(incomingConfigJson) as JsonObject;
                var existingNode = JsonNode.Parse(existingDecryptedConfigJson) as JsonObject;
                if (incomingNode == null || existingNode == null) return incomingConfigJson;

                MergeObjects(incomingNode, existingNode);
                return incomingNode.ToJsonString();
            }
            catch
            {
                return incomingConfigJson;
            }
        }

        private static void MergeObjects(JsonObject incoming, JsonObject existing)
        {
            foreach (var (propName, propVal) in incoming.ToList())
            {
                if (propVal is JsonValue val && val.TryGetValue<string>(out var strVal) && strVal == MaskValue)
                {
                    if (existing.TryGetPropertyValue(propName, out var existingVal) && existingVal is JsonValue existVal && existVal.TryGetValue<string>(out var existStr))
                    {
                        incoming[propName] = existStr;
                    }
                }
                else if (propVal is JsonObject nestedIncoming && existing.TryGetPropertyValue(propName, out var nestedExisting) && nestedExisting is JsonObject nestedExistObj)
                {
                    MergeObjects(nestedIncoming, nestedExistObj);
                }
            }
        }

        public static void ValidateSecretProviderConfig(SecretProviderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProviderName))
            {
                throw new ArgumentException("ProviderName is required");
            }

            if (!string.IsNullOrWhiteSpace(dto.ConfigJson))
            {
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(dto.ConfigJson);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("ConfigJson must be a valid JSON object.", ex);
                }

                using (doc)
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new ArgumentException("ConfigJson must be a JSON object.");
                    }

                    SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                    var name = dto.ProviderName.Trim();
                    if (string.Equals(name, "Vault", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "HashiCorpVault", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateVaultConfig(doc.RootElement);
                    }
                }
            }
        }

        private static void ValidateVaultConfig(JsonElement root)
        {
            string? roleId = null;
            string? secretId = null;
            string? token = null;

            if (root.TryGetProperty("roleId", out var rProp) || root.TryGetProperty("role_id", out rProp))
            {
                roleId = rProp.GetString();
            }
            if (root.TryGetProperty("secretId", out var sProp) || root.TryGetProperty("secret_id", out sProp))
            {
                secretId = sProp.GetString();
            }
            if (root.TryGetProperty("token", out var tProp) || root.TryGetProperty("vault_token", out tProp))
            {
                token = tProp.GetString();
            }

            if (!string.IsNullOrEmpty(roleId) && string.IsNullOrEmpty(secretId) && string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Vault AppRole authentication requires both RoleId and SecretId.");
            }
        }

        public static void ValidateAuthProviderConfig(AuthProviderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProviderName))
            {
                throw new ArgumentException("ProviderName is required");
            }

            if (!string.IsNullOrWhiteSpace(dto.ConfigJson))
            {
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(dto.ConfigJson);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("ConfigJson must be a valid JSON object.", ex);
                }

                using (doc)
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new ArgumentException("ConfigJson must be a JSON object.");
                    }

                    SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                    var name = dto.ProviderName.Trim();
                    if (string.Equals(name, "ActiveDirectory", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "LDAP", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateLdapConfig(doc.RootElement);
                    }
                }
            }
        }

        private static void ValidateLdapConfig(JsonElement root)
        {
            int port = 636;
            if (root.TryGetProperty("port", out var pProp))
            {
                if (pProp.ValueKind == JsonValueKind.Number && pProp.TryGetInt32(out var pNum))
                {
                    port = pNum;
                }
                else if (pProp.ValueKind == JsonValueKind.String && int.TryParse(pProp.GetString(), out var pStr))
                {
                    port = pStr;
                }
            }

            bool useSsl = port == 636;
            if (root.TryGetProperty("useSsl", out var sslProp) || root.TryGetProperty("use_ssl", out sslProp))
            {
                if (sslProp.ValueKind == JsonValueKind.True || sslProp.ValueKind == JsonValueKind.False)
                {
                    useSsl = sslProp.GetBoolean();
                }
            }

            if (port == 389 && !useSsl)
            {
                throw new ArgumentException("LDAP over plaintext (port 389) is disabled for security. Configure useSsl=true or use LDAPS port 636.");
            }
        }
    }
}

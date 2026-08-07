using McpRouter.Core.Logging;
using Xunit;

namespace McpRouter.Tests
{
    public class PiiSanitizerTests
    {
        [Fact]
        public void SanitizePayload_Redacts_Bearer_Tokens()
        {
            string raw = "{\"headers\": {\"Authorization\": \"Bearer secret_token_xyz_123\"}}";
            string clean = PiiSanitizer.SanitizePayload(raw);

            Assert.DoesNotContain("secret_token_xyz_123", clean);
            Assert.Contains("[REDACTED]", clean);
        }

        [Fact]
        public void SanitizePayload_Redacts_Api_Keys_And_Passwords()
        {
            string raw = "{\"apiKey\":\"my_secret_key\",\"password\":\"super_secret\"}";
            string clean = PiiSanitizer.SanitizePayload(raw);

            Assert.DoesNotContain("my_secret_key", clean);
            Assert.DoesNotContain("super_secret", clean);
            Assert.Contains("\"apiKey\":\"[REDACTED]\"", clean);
            Assert.Contains("\"password\":\"[REDACTED]\"", clean);
        }

        [Fact]
        public void SanitizePayload_Redacts_ConnectionString_Passwords()
        {
            string raw = "Data Source=mcp_router.db;Password=MySecretPassword123;Version=3;";
            string clean = PiiSanitizer.SanitizePayload(raw);

            Assert.DoesNotContain("MySecretPassword123", clean);
            Assert.Contains("Password=[REDACTED]", clean);
        }
    }
}

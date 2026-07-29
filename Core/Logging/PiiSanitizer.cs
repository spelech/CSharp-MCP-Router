using System.Text.RegularExpressions;

namespace McpRouter.Core.Logging
{
    public static class PiiSanitizer
    {
        private static readonly Regex TokenRegex = new(@"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex KeyRegex = new(@"""(api[-_]?key|password|secret|token|authorization)""\s*:\s*""[^""]+""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string SanitizePayload(string rawPayload)
        {
            if (string.IsNullOrEmpty(rawPayload)) return rawPayload;

            var sanitized = TokenRegex.Replace(rawPayload, "Bearer [REDACTED]");
            sanitized = KeyRegex.Replace(sanitized, "\"$1\":\"[REDACTED]\"");

            return sanitized;
        }
    }
}

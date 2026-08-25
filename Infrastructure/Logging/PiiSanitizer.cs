using System.Text.RegularExpressions;

namespace ModelContextGateway.Infrastructure.Logging
{
    public static class PiiSanitizer
    {
        private static readonly Regex TokenRegex = new(@"(Bearer|Basic)\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HeaderRegex = new(@"(?im)^(Authorization|X-Api-Key|Api-Key|Cookie|Set-Cookie)\s*:\s*.+$", RegexOptions.Compiled);
        private static readonly Regex KeyRegex = new(@"""(api[-_]?key|password|secret|token|access_token|authorization)""\s*:\s*""[^""]+""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex QueryRegex = new(@"(?i)([?&](?:access_token|token|api[-_]?key|key)=)[^&\s""]+", RegexOptions.Compiled);
        private static readonly Regex UserInfoRegex = new(@"(?i)(https?://)[^/\s:@]+:[^/\s@]+@", RegexOptions.Compiled);
        private static readonly Regex ConnStringPasswordRegex = new(@"Password\s*=\s*[^;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string SanitizePayload(string rawPayload)
        {
            if (string.IsNullOrEmpty(rawPayload))
            {
                return rawPayload;
            }

            var s = TokenRegex.Replace(rawPayload, "$1 [REDACTED]");
            s = HeaderRegex.Replace(s, m => m.Value.Split(':')[0] + ": [REDACTED]");
            s = KeyRegex.Replace(s, "\"$1\":\"[REDACTED]\"");
            s = QueryRegex.Replace(s, "$1[REDACTED]");
            s = UserInfoRegex.Replace(s, "$1[REDACTED]@");
            s = ConnStringPasswordRegex.Replace(s, "Password=[REDACTED]");
            s = s.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
            return s;
        }
    }
}

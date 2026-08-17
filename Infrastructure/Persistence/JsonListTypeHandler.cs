using System.Collections.Generic;
using Dapper;

namespace McpRouter.Infrastructure.Persistence
{
    public class JsonListTypeHandler : SqlMapper.TypeHandler<List<string>>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, List<string>? value)
        {
            parameter.Value = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
        }

        public override List<string> Parse(object value)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(str) ?? new List<string>(); }
                catch { }
            }
            return new List<string>();
        }
    }
}

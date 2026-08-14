using System.Runtime.CompilerServices;
using Dapper;

namespace McpRouter.Models
{
    internal static class DapperTypeHandlerInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            try
            {
                SqlMapper.AddTypeHandler(new McpRouter.Infrastructure.Persistence.JsonListTypeHandler());
            }
            catch { }
        }
    }
}

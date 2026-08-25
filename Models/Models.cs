using System.Runtime.CompilerServices;
using Dapper;

namespace ModelContextGateway.Models
{
    internal static class DapperTypeHandlerInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            try
            {
                SqlMapper.AddTypeHandler(new ModelContextGateway.Infrastructure.Persistence.JsonListTypeHandler());
            }
            catch { }
        }
    }
}

using System.Runtime.CompilerServices;
using Dapper;

namespace ModelContextGateway.Models
{
    internal static class DapperTypeHandlerInitializer
    {
        private static int _initialized;

        [ModuleInitializer]
        public static void Initialize()
        {
            if (System.Threading.Interlocked.Exchange(ref _initialized, 1) != 0)
            {
                return;
            }

            try
            {
                SqlMapper.AddTypeHandler(new ModelContextGateway.Infrastructure.Persistence.JsonListTypeHandler());
            }
            catch (System.Exception ex)
            {
                System.Console.Error.WriteLine($"[ModelContextGateway] Failed to register Dapper type handlers: {ex}");
            }
        }
    }
}

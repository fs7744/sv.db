using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitSQLite()
        {
            ConnectionFactory.RegisterConnectionProvider("MSSql", new MSSqlConnectionProvider());
        }

        public static IServiceCollection AddSQLite(this IServiceCollection services)
        {
            InitSQLite();
            return services;
        }
    }
}
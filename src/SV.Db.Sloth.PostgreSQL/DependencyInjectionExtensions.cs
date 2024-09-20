using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitSQLite()
        {
            ConnectionFactory.RegisterConnectionProvider("PostgreSQL", new PostgreSQLConnectionProvider());
        }

        public static IServiceCollection AddSQLite(this IServiceCollection services)
        {
            InitSQLite();
            return services;
        }
    }
}
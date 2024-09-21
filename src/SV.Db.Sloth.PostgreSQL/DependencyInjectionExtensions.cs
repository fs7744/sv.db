using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitSQLite()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.PostgreSQL))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.PostgreSQL, new PostgreSQLConnectionProvider());
        }

        public static IServiceCollection AddSQLite(this IServiceCollection services)
        {
            InitSQLite();
            return services;
        }
    }
}
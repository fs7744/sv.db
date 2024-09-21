using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitPostgreSQL()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.PostgreSQL))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.PostgreSQL, new PostgreSQLConnectionProvider());
        }

        public static IServiceCollection AddPostgreSQL(this IServiceCollection services)
        {
            InitPostgreSQL();
            return services;
        }
    }
}
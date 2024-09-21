using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitSQLite()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.MSSql))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.MSSql, new MSSqlConnectionProvider());
        }

        public static IServiceCollection AddSQLite(this IServiceCollection services)
        {
            InitSQLite();
            return services;
        }
    }
}
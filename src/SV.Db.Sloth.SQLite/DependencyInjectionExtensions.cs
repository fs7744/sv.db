using SV.Db.Sloth.SQLite;
using SV.Db;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitSQLite()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.SQLite))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.SQLite, new SQLiteConnectionProvider());
        }

        public static IServiceCollection AddSQLite(this IServiceCollection services)
        {
            InitSQLite();
            return services;
        }
    }
}
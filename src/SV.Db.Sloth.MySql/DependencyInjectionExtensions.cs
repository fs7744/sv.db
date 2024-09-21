using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitMySql()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.MySql))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.MySql, new MySqlConnectionProvider());
        }

        public static IServiceCollection AddMySql(this IServiceCollection services)
        {
            InitMySql();
            return services;
        }
    }
}
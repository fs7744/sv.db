using SV.Db;
using SV.Db.Sloth.MySql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitMSSql()
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.MSSql))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.MSSql, new MSSqlConnectionProvider());
        }

        public static IServiceCollection AddMSSql(this IServiceCollection services)
        {
            InitMSSql();
            return services;
        }
    }
}
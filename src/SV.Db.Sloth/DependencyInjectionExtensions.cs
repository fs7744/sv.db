using SV.Db;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static IServiceCollection AddConnectionStringProvider(this IServiceCollection services, params Func<IServiceProvider, IConnectionStringProvider>[] funcs)
        {
            return services.AddSingleton<IConnectionFactory>(i => new ConnectionStringProviders(funcs.Select(j => j(i)).ToArray(), i.GetService<IDbEntityInfoProvider>(), i));
        }
    }
}
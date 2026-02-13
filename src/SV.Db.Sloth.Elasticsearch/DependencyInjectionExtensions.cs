using Microsoft.Extensions.DependencyInjection.Extensions;
using SV.Db;
using SV.Db.Sloth.Elasticsearch;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static void InitElasticsearch(string key)
        {
            if (!ConnectionFactory.HasType(ConnectionStringProvider.Elasticsearch))
                ConnectionFactory.RegisterConnectionProvider(ConnectionStringProvider.Elasticsearch, new ElasticsearchConnectionProvider(key));
        }

        public static IServiceCollection AddElasticsearch(this IServiceCollection services, string key = ConnectionStringProvider.Elasticsearch, Action<HttpClient> config = null)
        {
            InitElasticsearch(key);
            services.TryAddSingleton<IEsClient>(i => ConnectionFactory.GetProvider(ConnectionStringProvider.Elasticsearch) as IEsClient ?? new ElasticsearchConnectionProvider(key));
            services.AddHttpClient(key, httpClient =>
            {
                config?.Invoke(httpClient);
            });
            return services;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;

namespace SV.Db.Sloth.Elasticsearch
{
    public partial class ElasticsearchConnectionProvider : IConnectionProvider
    {
        private string key;
        private IHttpClientFactory clientFactory;

        public ElasticsearchConnectionProvider(string key)
        {
            this.key = key;
        }

        public void Init(IServiceProvider provider)
        {
            clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        }

        private HttpClient CreateClient()
        {
            return clientFactory.CreateClient(key);
        }
    }
}
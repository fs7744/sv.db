using SV.Db.Sloth.SqlParser;
using SV.Db.Sloth.Statements;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SV.Db.Sloth.Elasticsearch
{
    public partial class ElasticsearchConnectionProvider : IConnectionProvider
    {
        public Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteUpdateAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public PageResult<T> ExecuteQuery<T>(string connectionString, DbEntityInfo info, SelectStatement statement)
        {
            return ExecuteQueryAsync<T>(connectionString, info, statement).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public async Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            var client = CreateClient();
            client.BaseAddress = new Uri(connectionString);
            var query = new ESQuery();
            var fs = statement.Fields;
            if (fs?.Any(i => i is FieldStatement f && f.Field.Equals("*")) != true)
            {
                query.fields = fs.Select(i => i.Field).ToArray();
            }
            var resp = await client.PostAsJsonAsync($"{info.Table}/_search", query, options, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var r = await resp.Content.ReadFromJsonAsync<ESResult<T>>(cancellationToken);
            var rr = new PageResult<T>();
            rr.TotalCount = r?.hits?.total?.value;
            if (rr.TotalCount <= 0) return rr;
            if (typeof(T) == typeof(object))
            {
                rr.Rows = r.hits.hits.Select(i => (T)(i.fields ?? i._source)).ToList();
            }
            else
            {
                rr.Rows = r.hits.hits.Select(i => i._source).ToList();
            }
            return rr;
        }
    }

    public class ESQuery
    {
        public string[] fields { get; set; }

        public bool track_total_hits { get; set; } = true;

        public object query { get; set; }
    }

    public class ESResultRow<T>
    {
        public T _source { get; set; }

        public object fields { get; set; }
    }

    public class ESResultTotal
    {
        public int value { get; set; }
    }

    public class ESResultHits<T>
    {
        public ESResultTotal total { get; set; }
        public List<ESResultRow<T>> hits { get; set; }
    }

    public class ESResult<T>
    {
        public ESResultHits<T> hits { get; set; }
    }
}
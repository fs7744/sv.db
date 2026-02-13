using SV.Db.Sloth.Statements;
using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SV.Db.Sloth.Elasticsearch
{
    public enum EsBulkAction
    {
        Create,
        Update,
        Delete
    }

    public interface IEsClient
    {
        Task<BulkResponse> BulkAsync<T>(string connectionString, string index, Func<T, (EsBulkAction action, string id)> func, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken);

        Task<BulkResponse> BulkDeleteAsync(string connectionString, string index, IEnumerable<string> data, int batchSize, CancellationToken cancellationToken);
    }

    public partial class ElasticsearchConnectionProvider : IConnectionProvider, IEsClient
    {
        public Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(connectionString, info, new[] { data }, 1, cancellationToken);
        }

        public async Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken)
        {
            Func<object, object> f = info.GetPrimaryKeyValue;
            if (f == null)
                f = (object o) => null;
            var res = await BulkAsync<T>(connectionString, info.Table, (d) =>
            {
                var k = f(d);
                return (EsBulkAction.Create, k?.ToString());
            }, data, batchSize, cancellationToken).ConfigureAwait(false);

            return res.Took;
        }

        public Task<int> ExecuteUpdateAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            return ExecuteUpdateAsync(connectionString, info, new[] { data }, 1, cancellationToken);
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
            if (statement.Offset.HasValue)
            {
                query.from = statement.Offset.Value;
            }
            query.size = statement.Rows;
            if (statement.OrderBy.IsNotNullOrEmpty())
            {
                query.sort = statement.OrderBy.Select(i => new KeyValuePair<string, string>(i.Field, (i is IOrderByField orderBy ? orderBy.Direction : OrderByDirection.Asc) == OrderByDirection.Asc ? "asc" : "desc")).ToDictionary();
            }
            if (statement.Where != null && statement.Where.Condition != null)
            {
                query.query = BuildCondition(info, statement.Where.Condition);
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

        private object BuildCondition(DbEntityInfo info, ConditionStatement condition)
        {
            if (condition is OperaterStatement os)
            {
                return BuildOperaterStatement(os, info);
            }
            else if (condition is UnaryOperaterStatement uo)
            {
                if (uo.Operater == "not")
                {
                    object r = new { must_not = BuildCondition(info, uo.Right) };
                    return new ESBool() { b = r };
                }
                throw new NotImplementedException(uo.Operater);
            }
            else if (condition is InOperaterStatement io)
            {
                return BuildInOperaterStatement(io, info);
            }
            else if (condition is ConditionsStatement conditions)
            {
                object r;
                if (conditions.Condition == Condition.And)
                {
                    r = new { must = new object[] { BuildCondition(info, conditions.Left), BuildCondition(info, conditions.Right) } };
                }
                else
                {
                    r = new { should = new object[] { BuildCondition(info, conditions.Left), BuildCondition(info, conditions.Right) } };
                }
                return new ESBool() { b = r };
            }
            throw new NotImplementedException();
        }

        private static object BuildOperaterStatement(OperaterStatement os, DbEntityInfo info)
        {
            var v = os.Right;
            if (os.Left is not FieldStatement f)
            {
                f = os.Right as FieldStatement;
                v = os.Left;
            }

            object r;
            switch (os.Operater)
            {
                case "is-null":
                    r = new
                    {
                        must_not = new { exists = new { field = f.Field } }
                    };
                    break;

                case "not-null":
                    r = new { must = new { exists = new { field = f.Field } } };
                    break;

                case "like":
                    r = new { must = new { wildcard = new Dictionary<string, object> { { f.Field, new { value = $"*{BuildValueStatement(v, info)}*", boost = 1.0 } } } } };
                    break;

                case "prefix-like":
                    r = new { must = new { prefix = new Dictionary<string, object> { { f.Field, new { value = BuildValueStatement(v, info) } } } } };
                    break;

                case "suffix-like":
                    r = new { must = new { wildcard = new Dictionary<string, object> { { f.Field, new { value = $"*{BuildValueStatement(v, info)}", boost = 1.0 } } } } };
                    break;

                case "=":
                    r = new { must = new { term = new Dictionary<string, object> { { f.Field, BuildValueStatement(v, info) } } } };
                    break;

                case "!=":
                    r = new { must_not = new { term = new Dictionary<string, object> { { f.Field, BuildValueStatement(v, info) } } } };
                    break;

                case ">":
                    r = new { must = new { range = new Dictionary<string, object> { { f.Field, new { gt = BuildValueStatement(v, info) } } } } };
                    break;

                case ">=":
                    r = new { must = new { range = new Dictionary<string, object> { { f.Field, new { gte = BuildValueStatement(v, info) } } } } };
                    break;

                case "<":
                    r = new { must = new { range = new Dictionary<string, object> { { f.Field, new { lt = BuildValueStatement(v, info) } } } } };
                    break;

                case "<=":
                    r = new { must = new { range = new Dictionary<string, object> { { f.Field, new { lte = BuildValueStatement(v, info) } } } } };
                    break;

                default:
                    throw new NotImplementedException(os.Operater);
            }

            return new ESBool() { b = r };
        }

        private static object BuildInOperaterStatement(InOperaterStatement io, DbEntityInfo info)
        {
            var array = io.Right;
            object v;
            if (array is StringArrayValueStatement s)
            {
                v = s.Value;
            }
            else if (array is BooleanArrayValueStatement b)
            {
                v = b.Value;
            }
            else if (array is NumberArrayValueStatement n)
            {
                v = n.Value;
            }
            else
            {
                throw new NotImplementedException(array.ToString());
            }
            return new ESBool() { b = new { must = new { terms = new Dictionary<string, object> { { io.Left is FieldStatement f ? f.Field : "", v } } } } };
        }

        private static object BuildValueStatement(ValueStatement v, DbEntityInfo info)
        {
            if (v is StringValueStatement s)
            {
                return s.Value;
            }
            else if (v is BooleanValueStatement b)
            {
                return b.Value;
            }
            else if (v is NumberValueStatement n)
            {
                return n.Value;
            }
            throw new NotImplementedException(v.ToString());
        }

        public async Task<R> ExecuteInsertRowAsync<T, R>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            Func<object, object> f = info.GetPrimaryKeyValue;
            if (f == null)
                f = (object o) => null;
            var k = f(data);
            var res = await BulkAsync<T>(connectionString, info.Table, (d) =>
            {
                return (EsBulkAction.Create, k?.ToString());
            }, new[] { data }, 1, cancellationToken).ConfigureAwait(false);

            return (R)k;
        }

        public async Task<int> ExecuteUpdateAsync<T>(string connectionString, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken)
        {
            Func<object, object> f = info.GetPrimaryKeyValue;
            if (f == null)
                f = (object o) => null;
            var res = await BulkAsync<T>(connectionString, info.Table, (d) =>
            {
                var k = f(d);
                return (EsBulkAction.Update, k?.ToString());
            }, data, batchSize, cancellationToken).ConfigureAwait(false);

            return res.Took;
        }

        private static readonly byte[] br = System.Text.Encoding.UTF8.GetBytes("\n");
        public async Task<BulkResponse> BulkAsync<T>(string connectionString, string index, Func<T, (EsBulkAction action, string id)> func, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken)
        {
            var client = CreateClient();
            if (!connectionString.EndsWith("/_bulk", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += "/_bulk";
            }
            var rr = new BulkResponse() { Items = new List<BulkInfo>() };
            foreach (var items in data.Chunk(batchSize))
            {
                var content = new PushStreamContent(async (stream, content, context) =>
                {
                    //var s = new MemoryStream();
                    await WriteData(index, func, stream, items);
                    //s.Seek(0, SeekOrigin.Begin);
                    //var dd = System.Text.Encoding.UTF8.GetString(s.ToArray());
                    //s.Seek(0, SeekOrigin.Begin);
                    //await s.CopyToAsync(stream);
                });
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var resp = await client.PostAsync(connectionString, content, cancellationToken);
                resp.EnsureSuccessStatusCode();
                var r = await resp.Content.ReadFromJsonAsync<BulkResponse>(cancellationToken);
                rr.Took += rr.Took;
                rr.Errors = rr.Errors || r.Errors;
                if (r.Items.IsNotNullOrEmpty())
                    rr.Items.AddRange(r.Items);
            }

            return rr;
        }

        private static async Task WriteData<T>(string index, Func<T, (EsBulkAction action, string id)> func, Stream stream, T[] items)
        {
            var wr = new Utf8JsonWriter(stream);
            foreach (var d in items)
            {
                var (action, id) = func(d);
                switch (action)
                {
                    case EsBulkAction.Create:
                        {
                            wr.WriteStartObject();
                            wr.WritePropertyName("create");
                            wr.WriteStartObject();
                            wr.WriteString("_index", index);
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                wr.WriteString("_id", id);
                            }
                            wr.WriteEndObject();
                            wr.WriteEndObject();
                            await wr.FlushAsync();
                            wr.Reset();
                            stream.Write(br);
                            JsonSerializer.Serialize(wr, d, options);
                            await wr.FlushAsync();
                            wr.Reset();
                            stream.Write(br);
                        }
                        break;
                    case EsBulkAction.Update:
                        {
                            if (string.IsNullOrWhiteSpace(id))
                                throw new ArgumentNullException(nameof(id), "Id cannot be null or empty for delete action.");

                            wr.WriteStartObject();
                            wr.WritePropertyName("update");
                            wr.WriteStartObject();
                            wr.WriteString("_index", index);
                            wr.WriteString("_id", id);
                            wr.WriteEndObject();
                            wr.WriteEndObject();
                            await wr.FlushAsync();
                            wr.Reset();
                            stream.Write(br);
                            wr.WriteStartObject();
                            wr.WritePropertyName("doc");
                            JsonSerializer.Serialize(wr, d, options);
                            wr.WriteEndObject();
                            await wr.FlushAsync();
                            wr.Reset();
                            stream.Write(br);
                        }
                        break;
                    case EsBulkAction.Delete:
                        {
                            //var wr = new Utf8JsonWriter(stream);
                            if (string.IsNullOrWhiteSpace(id))
                                throw new ArgumentNullException(nameof(id), "Id cannot be null or empty for delete action.");
                            wr.WriteStartObject();
                            wr.WritePropertyName("delete");
                            wr.WriteStartObject();
                            wr.WriteString("_index", index);
                            wr.WriteString("_id", id);
                            wr.WriteEndObject();
                            wr.WriteEndObject();
                            await wr.FlushAsync();
                            wr.Reset();
                            stream.Write(br);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public async Task<BulkResponse> BulkDeleteAsync(string connectionString, string index, IEnumerable<string> data, int batchSize, CancellationToken cancellationToken)
        {
            var res = await BulkAsync<string>(connectionString, index, (d) =>
            {
                return (EsBulkAction.Delete, d?.ToString());
            }, data, batchSize, cancellationToken).ConfigureAwait(false);

            return res;
        }
    }

    public class ESBool
    {
        [JsonPropertyName("bool")]
        public object b { get; set; }
    }

    public class ESQuery
    {
        public string[] fields { get; set; }

        public bool track_total_hits { get; set; } = true;

        public object query { get; set; }

        public int? from { get; set; }

        public int size { get; set; }
        public Dictionary<string, string> sort { get; set; }
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

    public class BulkResponse
    {
        [JsonPropertyName("took")]
        public int Took { get; set; }

        [JsonPropertyName("errors")]
        public bool Errors { get; set; }

        [JsonPropertyName("items")]
        public List<BulkInfo> Items { get; set; }
    }

    public class BulkInfo
    {
        [JsonPropertyName("index")]
        public BulkUpdateInfo Index { get; set; }

        [JsonPropertyName("delete")]
        public BulkUpdateInfo Delete { get; set; }

        [JsonPropertyName("create")]
        public BulkUpdateInfo Create { get; set; }

        [JsonPropertyName("update")]
        public BulkUpdateInfo Update { get; set; }
    }

    public class BulkUpdateInfo
    {
        [JsonPropertyName("_index")]
        public string Index { get; set; }

        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("_version")]
        public int Version { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("_shards")]
        public BulkUpdateShards Shards { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("_seq_no")]
        public int SeqNo { get; set; }

        [JsonPropertyName("_primary_term")]
        public int PrimaryTerm { get; set; }
    }

    public class BulkUpdateShards
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("successful")]
        public int Successful { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }
    }
}
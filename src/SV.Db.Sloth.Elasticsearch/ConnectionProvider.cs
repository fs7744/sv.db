using SV.Db.Sloth.Statements;
using System.Data;
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
}
using Microsoft.Extensions.Primitives;
using SV.Db.Sloth.SqlParser;
using SV.Db.Sloth.Statements;
using System.Collections.Frozen;
using System.Text.Json;

namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static PageResult<T> ExecuteQuery<T>(this SelectStatementBuilder builder, SelectStatementOptions? options = null)
        {
            SelectStatement statement = builder.Build(options);
            return builder.factory.ExecuteQuery<T>(builder.dbEntityInfo, statement);
        }

        public static Task<PageResult<T>> ExecuteQueryAsync<T>(this SelectStatementBuilder builder, SelectStatementOptions? options = null, CancellationToken cancellationToken = default)
        {
            SelectStatement statement = builder.Build(options);
            return builder.factory.ExecuteQueryAsync<T>(builder.dbEntityInfo, statement, cancellationToken);
        }

        public static SelectStatement ParseByParams<T>(this IConnectionFactory factory, IDictionary<string, StringValues> ps, out DbEntityInfo info, SelectStatementOptions? options = null)
        {
            var builder = factory.From<T>();
            ParseFields(ps, builder);
            ParseOrderBy(ps, builder);
            ParseGroupBy(ps, builder);
            ParsePage(ps, builder);
            ParseWhere(ps, builder);
            info = builder.dbEntityInfo;
            return builder.Build(options);
        }

        public static SelectStatement ParseByParams(this IConnectionFactory factory, string key, IDictionary<string, StringValues> ps, out DbEntityInfo info, SelectStatementOptions? options = null)
        {
            var builder = factory.From(key);
            ParseFields(ps, builder);
            ParseOrderBy(ps, builder);
            ParseGroupBy(ps, builder);
            ParsePage(ps, builder);
            ParseWhere(ps, builder);
            info = builder.dbEntityInfo;
            return builder.Build(options);
        }

        private static void ParseWhere(IDictionary<string, StringValues> ps, SelectStatementBuilder builder)
        {
            var where = new WhereStatement();
            where.Condition = ParseComplexWhere(ps);
            foreach (var kv in ps)
            {
                foreach (var v in kv.Value)
                {
                    var op = ParseOperaterStatement(kv.Key, v);
                    if (where.Condition == null)
                    {
                        where.Condition = op;
                    }
                    else
                    {
                        where.Condition = new ConditionsStatement() { Condition = Condition.And, Left = op, Right = where.Condition };
                    }
                }
            }
            if (where.Condition != null)
            {
                builder.statement.Where = where;
            }
        }

        private static ConditionStatement ParseComplexWhere(IDictionary<string, StringValues> ps)
        {
            if (ps.TryGetValue("Where", out var w))
            {
                ps.Remove("Where");
                var c = string.Join(" and ", w.Select(i => $" ({i}) "));
                return string.IsNullOrWhiteSpace(c) ? null : ParseWhereConditionStatement(c);
            }
            else
            {
                return null;
            }
        }

        private static ConditionStatement ParseWhereConditionStatement(string sql)
        {
            var s = SqlStatementParser.ParseStatements(sql).ToArray();
            if (s.Length > 1 || s.Length < 1 || s[0] is not ConditionStatement c)
            {
                throw new ParserExecption($"Can't parse condition for {sql}");
            }
            return c;
        }

        private const int OperatorLength = 6;

        private static readonly FrozenDictionary<string, Func<string, string, ConditionStatement>> operators = new Dictionary<string, Func<string, string, ConditionStatement>>
        {
            { "{{nl}}", (k, v) => new OperaterStatement() { Operater = "is-null", Left = new FieldStatement() { Field = k } } },
            { "{{eq}}", (k, v) => new OperaterStatement() { Operater = "=", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{lt}}", (k, v) => new OperaterStatement() { Operater = "<=", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{le}}", (k, v) => new OperaterStatement() { Operater = "<", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{gr}}", (k, v) => new OperaterStatement() { Operater = ">", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{gt}}", (k, v) => new OperaterStatement() { Operater = ">=", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{nq}}", (k, v) => new OperaterStatement() { Operater = "!=", Left = new FieldStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{in}}", (k, v) => new InOperaterStatement() { Left = new FieldStatement() { Field = k }, Right = ConvertArrayStatement(v) } },
            { "{{lk}}", (k, v) => new OperaterStatement() { Operater = "prefix-like", Left = new FieldStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
            { "{{kk}}", (k, v) => new OperaterStatement() { Operater = "like", Left = new FieldStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
            { "{{rk}}", (k, v) => new OperaterStatement() { Operater = "suffix-like", Left = new FieldStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
            { "{{no}}", (k, v) => new UnaryOperaterStatement(){ Operater = "not", Right = ParseOperaterStatement(k,v) }}
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        private static ValueStatement ConvertValueStatement(string v)
        {
            if (v.Equals("null", StringComparison.OrdinalIgnoreCase))
                return new NullValueStatement();
            else if (decimal.TryParse(v, out var value))
                return new NumberValueStatement() { Value = value };
            else if (bool.TryParse(v, out var b))
                return new BooleanValueStatement() { Value = b };
            else if (v.StartsWith("\"") && v.EndsWith("\""))
                return new StringValueStatement() { Value = JsonSerializer.Deserialize<string>(v) };
            else
                return new StringValueStatement() { Value = v };
        }

        private static ArrayValueStatement ConvertArrayStatement(string v)
        {
            var array = JsonSerializer.Deserialize<List<object>>(v);
            if (array.IsNullOrEmpty()) throw new NotSupportedException($"Array can not be empty");
            var f = (JsonElement)array.First();
            switch (f.ValueKind)
            {
                case JsonValueKind.String:
                    return new StringArrayValueStatement() { Value = array.Select(i => ((JsonElement)i).GetString()).ToList() };

                case JsonValueKind.Number:
                    return new NumberArrayValueStatement() { Value = array.Select(i => ((JsonElement)i).GetDecimal()).ToList() };

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new BooleanArrayValueStatement() { Value = array.Select(i => ((JsonElement)i).GetBoolean()).ToList() };

                default:
                    throw new NotSupportedException(v);
            }
        }

        private static ConditionStatement ParseOperaterStatement(string key, string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                throw new NotSupportedException($"Field {key} can not be empty");
            var op = v.Length < OperatorLength || !v.StartsWith("{{") ? "{{eq}}" : v[0..OperatorLength];
            var vv = v;
            if (v.StartsWith("{{") && v.Length >= OperatorLength)
            {
                vv = v[OperatorLength..];
            }
            if (!operators.TryGetValue(op, out var opr))
            {
                throw new NotSupportedException($"Field operators no support {op}");
            }
            return opr(key, vv);
        }

        private static void ParsePage(IDictionary<string, StringValues> ps, SelectStatementBuilder builder)
        {
            if (ps.TryGetValue("Rows", out var psize) && int.TryParse(psize, out var ipsize))
            {
                ps.Remove("Rows");
                builder.statement.Rows = ipsize;
            }
            if (ps.TryGetValue("Offset", out var offset) && int.TryParse(offset, out var ioffset))
            {
                ps.Remove("Offset");
                builder.statement.Offset = ioffset;
            }
        }

        private static void ParseOrderBy(IDictionary<string, StringValues> ps, SelectStatementBuilder builder)
        {
            if (ps.TryGetValue("OrderBy", out var ob))
            {
                ps.Remove("OrderBy");
                var orderBy = SqlStatementParser.ParseStatements(ob.ToString(), ParseType.OrderByField).Cast<FieldStatement>().Reverse().ToList();
                if (orderBy.Count > 0)
                {
                    builder.statement.OrderBy = orderBy;
                }
            }
        }

        private static void ParseGroupBy(IDictionary<string, StringValues> ps, SelectStatementBuilder builder)
        {
            if (ps.TryGetValue("GroupBy", out var ob))
            {
                ps.Remove("GroupBy");
                var orderBy = SqlStatementParser.ParseStatements(ob.ToString(), ParseType.SelectField).Cast<FieldStatement>().ToList();
                if (orderBy.Count > 0)
                {
                    builder.statement.GroupBy = orderBy;
                }
            }
        }

        private static void ParseFields(IDictionary<string, StringValues> ps, SelectStatementBuilder builder)
        {
            List<FieldStatement> fields = null;
            var hasTotalCount = ps.TryGetValue("TotalCount", out var tc) && bool.TryParse(tc, out var htc) && htc;
            ps.Remove("TotalCount");
            if (hasTotalCount)
            {
                builder.statement.HasTotalCount = true;
            }
            var noRows = ps.TryGetValue("NoRows", out var nr) && bool.TryParse(nr, out var nnr) && nnr;
            ps.Remove("NoRows");
            if (noRows)
            {
                builder.statement.Fields = null;
            }
            else
            {
                if (ps.TryGetValue("Fields", out var fs))
                {
                    ps.Remove("Fields");
                    fields = SqlStatementParser.ParseStatements(fs, ParseType.SelectField | ParseType.GrGroupByFuncField).Cast<FieldStatement>().ToList();
                }
                else
                {
                    var f = new FieldStatement();
                    f.Field = "*";
                    fields = new List<FieldStatement>() { f };
                }
            }
            if (fields?.Count > 0)
            {
                builder.statement.Fields = fields;
            }
        }
    }
}
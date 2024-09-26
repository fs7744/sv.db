using Microsoft.Extensions.Primitives;
using SV.Db.Sloth.SqlParser;
using SV.Db.Sloth.Statements;
using System.Collections.Frozen;
using System.Text.Json;

namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static SelectStatement ParseByParams<T>(this IConnectionFactory factory, IDictionary<string, StringValues> ps, out DbEntityInfo info)
        {
            info = factory.GetDbEntityInfo<T>();
            var builder = ParseByParams<T>(ps).Build(info);
            return builder;
        }

        public static SelectStatementBuilder<T> ParseByParams<T>(IDictionary<string, StringValues> ps)
        {
            var builder = new SelectStatementBuilder<T>();
            ParseFields(ps, builder);
            ParseOrderBy(ps, builder);
            ParsePage(ps, builder);
            ParseWhere(ps, builder);
            return builder;
        }

        private static void ParseWhere<T>(IDictionary<string, StringValues> ps, SelectStatementBuilder<T> builder)
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
                var c = string.Join(" AND ", w.Select(i => $" ({i}) "));
                return string.IsNullOrWhiteSpace(c) ? null : SqlStatementParser.ParseWhereConditionStatement(c);
            }
            else
            {
                return null;
            }
        }

        private const int OperatorLength = 6;

        private static readonly FrozenDictionary<string, Func<string, string, ConditionStatement>> operators = new Dictionary<string, Func<string, string, ConditionStatement>>
        {
            { "{{nl}}", (k, v) => new OperaterStatement() { Operater = "is-null", Left = new FieldValueStatement() { Field = k } } },
            { "{{eq}}", (k, v) => new OperaterStatement() { Operater = "=", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{lt}}", (k, v) => new OperaterStatement() { Operater = "<=", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{le}}", (k, v) => new OperaterStatement() { Operater = "<", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{gr}}", (k, v) => new OperaterStatement() { Operater = ">", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{gt}}", (k, v) => new OperaterStatement() { Operater = ">=", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{nq}}", (k, v) => new OperaterStatement() { Operater = "!=", Left = new FieldValueStatement() { Field = k }, Right = ConvertValueStatement(v) } },
            { "{{in}}", (k, v) => new InOperaterStatement() { Left = new FieldValueStatement() { Field = k }, Right = ConvertArrayStatement(v) } },
            { "{{lk}}", (k, v) => new OperaterStatement() { Operater = "prefix-like", Left = new FieldValueStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
            { "{{kk}}", (k, v) => new OperaterStatement() { Operater = "like", Left = new FieldValueStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
            { "{{rk}}", (k, v) => new OperaterStatement() { Operater = "suffix-like", Left = new FieldValueStatement() { Field = k }, Right = new StringValueStatement() { Value = v } } },
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
            if (op.StartsWith("{{") && v.Length >= OperatorLength)
            {
                vv = v[OperatorLength..];
            }
            if (!operators.TryGetValue(op, out var opr))
            {
                throw new NotSupportedException($"Field operators no support {op}");
            }
            return opr(key, vv);
        }

        private static void ParsePage<T>(IDictionary<string, StringValues> ps, SelectStatementBuilder<T> builder)
        {
            if (ps.TryGetValue("Rows", out var psize) && int.TryParse(psize, out var ipsize))
            {
                ps.Remove("Rows");
                builder.statement.Limit.Rows = ipsize;
            }
            if (ps.TryGetValue("Offset", out var offset) && int.TryParse(offset, out var ioffset))
            {
                ps.Remove("Offset");
                builder.statement.Limit.Offset = ioffset;
            }
        }

        private static void ParseOrderBy<T>(IDictionary<string, StringValues> ps, SelectStatementBuilder<T> builder)
        {
            if (ps.TryGetValue("OrderBy", out var ob))
            {
                ps.Remove("OrderBy");
                var orderBy = new OrderByStatement() { Fields = new List<OrderByFieldStatement>() };
                foreach (var item in ob.ToString().Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    var f = new OrderByFieldStatement();
                    if (!item.Contains(":"))
                    {
                        var ff = item.Split(':', 2);
                        f.Name = ff[0];
                        f.Direction = Enums<OrderByDirection>.Parse(ff[1]);
                    }
                    else
                    {
                        f.Name = item;
                        f.Direction = OrderByDirection.Asc;
                    }
                    orderBy.Fields.Add(f);
                }
                if (orderBy.Fields.Count > 0)
                {
                    builder.statement.OrderBy = orderBy;
                }
            }
        }

        private static void ParseFields<T>(IDictionary<string, StringValues> ps, SelectStatementBuilder<T> builder)
        {
            var fields = new SelectFieldsStatement() { Fields = new List<FieldStatement>() };
            var hasTotalCount = ps.TryGetValue("TotalCount", out var tc) && bool.TryParse(tc, out var htc) && htc;
            ps.Remove("TotalCount");
            if (hasTotalCount)
            {
                fields.Fields.Add(new FuncCallerStatement() { Name = "count()" });
            }
            var noRows = ps.TryGetValue("NoRows", out var nr) && bool.TryParse(nr, out var nnr) && nnr;
            ps.Remove("NoRows");
            if (!noRows)
            {
                if (ps.TryGetValue("Fields", out var fs))
                {
                    ps.Remove("Fields");
                    foreach (var item in fs.ToString().Split(",", StringSplitOptions.RemoveEmptyEntries))
                    {
                        var f = new FieldStatement();
                        if (item.Contains(":"))
                        {
                            var ff = item.Split(':', 2);
                            f.Name = ff[0];
                            f.As = ff[1];
                        }
                        else
                        {
                            f.Name = item;
                        }
                        fields.Fields.Add(f);
                    }
                }
                else
                {
                    var f = new FieldStatement();
                    f.Name = "*";
                    fields.Fields.Add(f);
                }
            }
            if (fields.Fields.Count > 0)
            {
                builder.statement.Fields = fields;
            }
        }
    }
}
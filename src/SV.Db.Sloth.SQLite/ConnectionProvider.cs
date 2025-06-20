﻿using SV.Db.Sloth.SqlParser;
using SV.Db.Sloth.Statements;
using System.Data;
using System.Data.Common;
using System.Text;

namespace SV.Db.Sloth.SQLite
{
    public partial class SQLiteConnectionProvider : IConnectionProvider
    {
        public void Init(IServiceProvider provider)
        {
        }

        public Task<int> ExecuteUpdateAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            return Create(connectionString).ExecuteNonQueryAsync(CreateUpdateSql(info, data), data, cancellationToken);
        }

        public Task<R> ExecuteInsertRowAsync<T, R>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            return Create(connectionString).ExecuteScalarAsync<R>(info.GetInsertSql(CreateInsertSql), data, cancellationToken);
        }

        private string CreateUpdateSql<T>(DbEntityInfo info, T? data)
        {
            var sb = new StringBuilder();
            sb.Append("UPDATE ");
            sb.AppendLine(info.UpdateTable);
            sb.Append("SET ");
            var first = true;
            foreach (var item in info.GetUpdateFields(data))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append(item.Value);
                sb.Append(" = @");
                sb.Append(item.Key);
            }
            sb.Append(" WHERE ");
            first = true;
            foreach (var item in info.UpdateColumns.Where(i => i.Value.PrimaryKey))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(" and ");
                }
                sb.Append(item.Value.Field);
                sb.Append(" = @");
                sb.Append(item.Key);
            }
            return sb.ToString();
        }

        public Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken)
        {
            return Create(connectionString).ExecuteNonQueryAsync(info.GetInsertSql(CreateInsertSql), data, cancellationToken);
        }

        public Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken)
        {
            return Create(connectionString).ExecuteNonQuerysAsync(info.GetInsertSql(CreateInsertSql), data, batchSize, cancellationToken);
        }

        private string CreateInsertSql(DbEntityInfo info)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ");
            sb.AppendLine(info.UpdateTable);
            sb.Append("(");
            var first = true;
            foreach (var item in info.UpdateColumns.Where(i => !i.Value.NotAllowInsert))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append(item.Value.Field);
            }
            sb.Append(") VALUES(");
            first = true;
            foreach (var item in info.UpdateColumns.Where(i => !i.Value.NotAllowInsert))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append("@");
                sb.Append(item.Key);
            }
            sb.Append(");SELECT last_insert_rowid();");
            return sb.ToString();
        }

        public PageResult<T> ExecuteQuery<T>(string connectionString, DbEntityInfo info, SelectStatement statement)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            var jsonFields = BuildSelectStatement(cmd, info, statement, out var hasTotal, out var hasRows);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            using var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            var result = new PageResult<T>();
            if (hasTotal)
            {
                result.TotalCount = reader.QueryFirstOrDefault<int>();
            }
            if (hasRows)
            {
                if (jsonFields != null && typeof(T) == typeof(object))
                {
                    result.Rows = reader.Query<T>().Select(i =>
                    {
                        if (i is IDictionary<string, object?> dict)
                        {
                            foreach (var item in jsonFields)
                            {
                                if (dict.ContainsKey(item) && dict[item] is string str && !string.IsNullOrWhiteSpace(str))
                                {
                                    try
                                    {
                                        dict[item] = ConnectionFactory.ParseJsonToken(str);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                        return i;
                    }).AsList();
                }
                else
                {
                    result.Rows = reader.Query<T>().AsList();
                }
            }
            return result;
        }

        public async Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            var jsonFields = BuildSelectStatement(cmd, info, statement, out var hasTotal, out var hasRows);
            using var reader = await CommandExtensions.DbDataReaderAsync(cmd, CommandBehavior.CloseConnection, cancellationToken);
            var result = new PageResult<T>();
            if (hasTotal)
            {
                result.TotalCount = await reader.QueryFirstOrDefaultAsync<int>();
            }
            if (hasRows)
            {
                if (jsonFields != null && typeof(T) == typeof(object))
                {
                    result.Rows = await reader.QueryAsync<T>(cancellationToken).ToListAsync(i =>
                    {
                        if (i is IDictionary<string, object?> dict)
                        {
                            foreach (var item in jsonFields)
                            {
                                if (dict.ContainsKey(item) && dict[item] is string str && !string.IsNullOrWhiteSpace(str))
                                {
                                    try
                                    {
                                        dict[item] = ConnectionFactory.ParseJsonToken(str);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                        return i;
                    }, cancellationToken);
                }
                else
                {
                    result.Rows = await reader.QueryAsync<T>(cancellationToken).ToListAsync(cancellationToken);
                }
            }
            return result;
        }

        private ISet<string> BuildSelectStatement(DbCommand cmd, DbEntityInfo info, SelectStatement statement, out bool hasTotalCount, out bool hasRows)
        {
            cmd.CommandTimeout = info.Timeout;
            var fs = statement.Fields;
            ISet<string> jf = null;

            string table = info.Table;
            if (!table.Contains("{Where}", StringComparison.OrdinalIgnoreCase))
            {
                table += " {Where} ";
            }

            string tableTotal;
            if (statement.HasTotalCount)
            {
                if (table.Contains("{Fields}", StringComparison.OrdinalIgnoreCase))
                {
                    tableTotal = table.Replace("{Fields}", " count(*) ", StringComparison.OrdinalIgnoreCase) + " ; ";
                }
                else
                {
                    tableTotal = $"SELECT count(*) FROM {table} ; ";
                }
                hasTotalCount = true;
            }
            else
            {
                tableTotal = string.Empty;
                hasTotalCount = false;
            }

            if (fs.IsNotNullOrEmpty())
            {
                hasRows = true;
                if (!table.Contains("{Fields}", StringComparison.OrdinalIgnoreCase))
                {
                    table = $"SELECT {{Fields}} FROM {table} ";
                }
            }
            else
            {
                table = string.Empty;
                hasRows = false;
            }

            if (tableTotal.Contains("{OrderBy}", StringComparison.OrdinalIgnoreCase))
            {
                tableTotal = tableTotal.Replace("{OrderBy}", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (hasRows && !table.Contains("{OrderBy}", StringComparison.OrdinalIgnoreCase))
            {
                table += " {OrderBy} ";
            }

            table = tableTotal + table;

            if (hasRows)
            {
                jf = info.GetJsonFields();
                if (fs?.Any(i => i is FieldStatement f && f.Field.Equals("*")) == true)
                {
                    table = table.Replace("{Fields}", info.SelectAll((x, y) => $"{y} as {x}", ","), StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    jf = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    table = table.Replace("{Fields}", ConvertFields(info, fs, true, ParseType.SelectField, jf), StringComparison.OrdinalIgnoreCase);
                }
            }

            if (statement.Where == null || statement.Where.Condition == null)
            {
                table = table.Replace("{Where}", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                table = table.Replace("{Where}", BuildCondition(cmd, info, statement.Where.Condition), StringComparison.OrdinalIgnoreCase);
            }

            if (statement.GroupBy.IsNotNullOrEmpty())
            {
                table = table.Replace("{OrderBy}", $"group by {ConvertFields(info, statement.GroupBy, false, ParseType.OrderByField, null)} ");
            }
            else
            {
                if (statement.OrderBy == null || statement.OrderBy.IsNullOrEmpty())
                {
                    table = table.Replace("{OrderBy}", " {Limit} ", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    table = table.Replace("{OrderBy}", " order by " + ConvertFields(info, statement.OrderBy, false, ParseType.OrderByField, null) + " {Limit} ");
                }

                if (!statement.Offset.HasValue)
                {
                    statement.Offset = 0;
                }
                table = table.Replace("{Limit}", $"Limit {statement.Offset},{statement.Rows} ");
            }

            cmd.CommandText = table;
            return jf;
        }

        private string? ConvertFields(DbEntityInfo info, IEnumerable<FieldStatement>? fs, bool allowAs, ParseType parseType, ISet<string> jsonFields)
        {
            var sb = new StringBuilder();
            var notFirst = false;
            foreach (var item in fs)
            {
                if (notFirst)
                {
                    sb.Append(",");
                }
                else
                {
                    notFirst = true;
                }
                ConvertField(item, info, sb, allowAs, parseType, jsonFields);
            }
            return sb.ToString();
        }

        private static bool ConvertField(Statement v, DbEntityInfo info, StringBuilder sb, bool allowAs, ParseType parseType, ISet<string> jsonFields)
        {
            var fs = parseType switch
            {
                ParseType.OrderByField => info.OrderByFields,
                ParseType.Condition => info.WhereFields,
                ParseType.SelectField => info.SelectFields,
            };
            if (v is JsonFieldStatement js)
            {
                ConvertJsonField(v, sb, allowAs, fs, js);
                if (jsonFields != null && parseType == ParseType.SelectField && !string.IsNullOrWhiteSpace(js.As) && !jsonFields.Contains(js.As))
                {
                    jsonFields.Add(js.As);
                }
                return true;
            }
            else if (v is GroupByFuncFieldStatement g)
            {
                sb.Append(g.Func);
                sb.Append("(");
                sb.Append(fs[g.Field]);
                sb.Append(")");
                if (allowAs && !string.IsNullOrWhiteSpace(g.As))
                {
                    sb.Append(" as ");
                    sb.Append(g.As);
                }
                return true;
            }
            else if (v is FieldStatement f)
            {
                sb.Append(fs[f.Field]);
                if (allowAs)
                {
                    sb.Append(" as ");
                    sb.Append(f.Field);
                }
                if (v is IOrderByField orderBy)
                {
                    sb.Append(" ");
                    sb.Append(Enums<OrderByDirection>.GetName(orderBy.Direction));
                }
                if (jsonFields != null && parseType == ParseType.SelectField && !jsonFields.Contains(f.Field))
                {
                    var all = info.GetJsonFields();
                    if (all != null && all.Contains(f.Field))
                    {
                        jsonFields.Add(f.Field);
                    }
                }
                return true;
            }
            return false;
        }

        public class BuildConditionContext
        {
            public int ParamsCount;

            public string NewParamsName() => $"@P_{ParamsCount++}";
        }

        public static string BuildCondition(DbCommand cmd, DbEntityInfo info, ConditionStatement condition)
        {
            var sb = new StringBuilder();
            var context = new BuildConditionContext();
            BuildCondition(sb, cmd, info, condition, context);
            sb.Insert(0, "where");
            return sb.ToString();
        }

        private static void BuildCondition(StringBuilder sb, DbCommand cmd, DbEntityInfo info, ConditionStatement condition, BuildConditionContext context)
        {
            if (condition is OperaterStatement os)
            {
                BuildOperaterStatement(sb, os, cmd, info, context);
            }
            else if (condition is UnaryOperaterStatement uo)
            {
                BuildUnaryOperaterStatement(sb, uo, cmd, info, context);
            }
            else if (condition is InOperaterStatement io)
            {
                BuildInOperaterStatement(sb, io, cmd, info, context);
            }
            else if (condition is ConditionsStatement conditions)
            {
                if (conditions.Condition == Condition.And)
                {
                    sb.Append(" (");
                    BuildCondition(sb, cmd, info, conditions.Left, context);
                    sb.Append(" AND ");
                    BuildCondition(sb, cmd, info, conditions.Right, context);
                    sb.Append(") ");
                }
                else
                {
                    sb.Append(" (");
                    BuildCondition(sb, cmd, info, conditions.Left, context);
                    sb.Append(" OR ");
                    BuildCondition(sb, cmd, info, conditions.Right, context);
                    sb.Append(") ");
                }
            }
        }

        private static void BuildInOperaterStatement(StringBuilder sb, InOperaterStatement io, DbCommand cmd, DbEntityInfo info, BuildConditionContext context)
        {
            var f = io.Left as FieldStatement;
            if (f != null && info.WhereFields.TryGetValue(f.Field, out var w) && w.Contains("{field}", StringComparison.OrdinalIgnoreCase))
            {
                var ssb = new StringBuilder();
                ssb.Append(" in (");
                BuildArrayValueStatement(io.Right, ssb, cmd, info, io.Left as FieldStatement, context);
                ssb.Append(") ");
                sb.Append(' ');
                sb.Append(w.Replace("{field}", ssb.ToString(), StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                sb.Append(' ');
                BuildValueStatement(io.Left, sb, cmd, info, null, context, ParseType.Condition);
                sb.Append(" in (");
                BuildArrayValueStatement(io.Right, sb, cmd, info, io.Left as FieldStatement, context);
                sb.Append(") ");
            }
        }

        private static void BuildArrayValueStatement(ArrayValueStatement array, StringBuilder sb, DbCommand cmd, DbEntityInfo info, FieldStatement? fieldValueStatement, BuildConditionContext context)
        {
            if (array is StringArrayValueStatement s)
            {
                ColumnAttribute c = null;
                var f = fieldValueStatement != null && info.Columns != null && info.Columns.TryGetValue(fieldValueStatement.Field, out c);
                for (var i = 0; i < s.Value.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');

                    var p = cmd.CreateParameter();
                    p.ParameterName = context.NewParamsName();
                    if (f)
                    {
                        p.DbType = c.Type;
                        p.Direction = c.Direction;
                        if (c.Precision > 0)
                            p.Precision = c.Precision;
                        if (c.Size > 0)
                            p.Size = c.Size;
                        if (c.Scale > 0)
                            p.Scale = c.Scale;
                    }
                    else
                    {
                        p.DbType = DbType.String;
                    }
                    p.Value = s.Value[i];
                    cmd.Parameters.Add(p);
                    sb.Append(p.ParameterName);
                }
            }
            else if (array is BooleanArrayValueStatement b)
            {
                for (var i = 0; i < b.Value.Count; i++)
                {
                    var bb = b.Value[i];
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(bb ? "true" : "false");
                }
            }
            else if (array is NumberArrayValueStatement n)
            {
                for (var i = 0; i < n.Value.Count; i++)
                {
                    var bb = n.Value[i];
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(bb);
                }
            }
        }

        private static void BuildUnaryOperaterStatement(StringBuilder sb, UnaryOperaterStatement uo, DbCommand cmd, DbEntityInfo info, BuildConditionContext context)
        {
            if (uo.Operater == "not")
            {
                sb.Append(" not (");
                BuildCondition(sb, cmd, info, uo.Right, context);
                sb.Append(") ");
            }
        }

        public static string ReplaceLikeValue(string str)
        {
            return str.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[").Replace("^", "\\^");
        }

        private static void BuildOperaterStatement(StringBuilder sb, OperaterStatement os, DbCommand cmd, DbEntityInfo info, BuildConditionContext context)
        {
            var f = os.Left as FieldStatement;
            if (f == null)
            {
                f = os.Right as FieldStatement;
            }
            if (f != null && info.WhereFields.TryGetValue(f.Field, out var w) && w.Contains("{field}", StringComparison.OrdinalIgnoreCase))
            {
                var ssb = new StringBuilder();
                BuildOperaterStatementStr(ssb, os, cmd, info, context, os.Left == f ? os.Right : os.Left, f);
                sb.Append(' ');
                sb.Append(w.Replace("{field}", ssb.ToString(), StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                sb.Append(' ');
                BuildValueStatement(os.Left, sb, cmd, info, f, context, ParseType.Condition);
                sb.Append(' ');
                BuildOperaterStatementStr(sb, os, cmd, info, context, os.Right, f);
            }
        }

        private static void BuildOperaterStatementStr(StringBuilder sb, OperaterStatement os, DbCommand cmd, DbEntityInfo info, BuildConditionContext context, ValueStatement right, FieldStatement? f)
        {
            switch (os.Operater)
            {
                case "is-null":
                    sb.Append("IS NULL");
                    break;

                case "not-null":
                    sb.Append("IS NOT NULL");
                    break;

                case "like":
                    sb.Append("like ");
                    var rf = right as StringValueStatement;
                    sb.Append($"'%{ReplaceLikeValue(rf.Value)}%'");
                    break;

                case "prefix-like":
                    sb.Append("like ");
                    var lrf = right as StringValueStatement;
                    sb.Append($"'{ReplaceLikeValue(lrf.Value)}%'");
                    break;

                case "suffix-like":
                    sb.Append("like ");
                    var srf = right as StringValueStatement;
                    sb.Append($"'%{ReplaceLikeValue(srf.Value)}'");
                    break;

                default:
                    sb.Append(os.Operater);
                    sb.Append(' ');
                    BuildValueStatement(right, sb, cmd, info, f, context, ParseType.Condition);
                    break;
            }

            sb.Append(' ');
        }

        private static void BuildValueStatement(ValueStatement v, StringBuilder sb, DbCommand cmd, DbEntityInfo info, FieldStatement? fieldValueStatement, BuildConditionContext context, ParseType parseType)
        {
            if (ConvertField(v, info, sb, false, parseType, null))
            {
            }
            else if (v is StringValueStatement s)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = context.NewParamsName();
                if (fieldValueStatement != null && info.Columns != null && info.Columns.TryGetValue(fieldValueStatement.Field, out var c))
                {
                    p.DbType = c.Type;
                    p.Direction = c.Direction;
                    if (c.Precision > 0)
                        p.Precision = c.Precision;
                    if (c.Size > 0)
                        p.Size = c.Size;
                    if (c.Scale > 0)
                        p.Scale = c.Scale;
                }
                else
                {
                    p.DbType = DbType.String;
                }
                p.Value = s.Value;
                cmd.Parameters.Add(p);
                sb.Append(p.ParameterName);
            }
            else if (v is BooleanValueStatement b)
            {
                sb.Append(b.Value ? "true" : "false");
            }
            else if (v is NumberValueStatement n)
            {
                sb.Append(n.Value.ToString());
            }
        }
    }
}
using Npgsql;
using SV.Db.Sloth.Statements;
using System.Data;
using System.Data.Common;
using System.Text;

namespace SV.Db.Sloth.PostgreSQL
{
    public class PostgreSQLConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public PageResult<T> ExecuteQuery<T>(string connectionString, DbEntityInfo info, SelectStatement statement)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            BuildSelectStatement(cmd, info, statement, out var hasTotal, out var hasRows);
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
                result.Rows = reader.Query<T>().AsList();
            }
            return result;
        }

        public async Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            BuildSelectStatement(cmd, info, statement, out var hasTotal, out var hasRows);
            using var reader = await CommandExtensions.DbDataReaderAsync(cmd, CommandBehavior.CloseConnection, cancellationToken);
            var result = new PageResult<T>();
            if (hasTotal)
            {
                result.TotalCount = await reader.QueryFirstOrDefaultAsync<int>();
            }
            if (hasRows)
            {
                result.Rows = await reader.QueryAsync<T>(cancellationToken).ToListAsync(cancellationToken);
            }
            return result;
        }

        private void BuildSelectStatement(DbCommand cmd, DbEntityInfo info, SelectStatement statement, out bool hasTotalCount, out bool hasRows)
        {
            cmd.CommandTimeout = info.Timeout;
            var fs = statement.Fields?.Fields;

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
                hasRows = false;
            }

            if (table.Contains("{OrderBy}", StringComparison.OrdinalIgnoreCase))
            {
                tableTotal = tableTotal.Replace("{OrderBy}", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                table += " {OrderBy} ";
            }

            table = tableTotal + table;

            if (hasRows)
            {
                if (fs?.Any(i => i is FieldStatement f && f.Field.Equals("*")) == true)
                {
                    table = table.Replace("{Fields}", info.SelectAll, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    table = table.Replace("{Fields}", ConvertFields(info, fs, true), StringComparison.OrdinalIgnoreCase);
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

            if (statement.OrderBy == null || statement.OrderBy.Fields.IsNullOrEmpty())
            {
                table = table.Replace("{OrderBy}", " {Limit} ", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                table = table.Replace("{OrderBy}", " order by " + ConvertFields(info, statement.OrderBy.Fields, false) + " {Limit} ");
            }

            if (!statement.Offset.HasValue)
            {
                statement.Offset = 0;
            }
            table = table.Replace("{Limit}", $"Limit {statement.Offset},{statement.Rows} ");

            cmd.CommandText = table;
        }

        private string? ConvertFields(DbEntityInfo info, IEnumerable<FieldStatement>? fs, bool allowAs)
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
                ConvertField(item, info, sb, allowAs);
            }
            return sb.ToString();
        }

        private static bool ConvertField(Statement v, DbEntityInfo info, StringBuilder sb, bool allowAs)
        {
            if (v is JsonFieldStatement js)
            {
                sb.Append("jsonb_path_query_first(");
                sb.Append(info.SelectFields[js.Field]);
                sb.Append(",");
                sb.Append("'");
                sb.Append(js.Path.Replace("'", "\\'"));
                sb.Append("'");
                sb.Append(")");
                if (allowAs && !string.IsNullOrWhiteSpace(js.As))
                {
                    sb.Append(" as ");
                    sb.Append(js.As);
                }
                if (v is IOrderByField orderBy)
                {
                    sb.Append(" ");
                    sb.Append(Enums<OrderByDirection>.GetName(orderBy.Direction));
                }
                return true;
            }
            else if (v is FieldStatement f)
            {
                sb.Append(info.SelectFields[f.Field]);
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
                    BuildCondition(sb, cmd, info, conditions.Left, context);
                    sb.Append(" AND ");
                    BuildCondition(sb, cmd, info, conditions.Right, context);
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
            sb.Append(' ');
            BuildValueStatement(io.Left, sb, cmd, info, null, context);
            sb.Append(" in (");
            BuildArrayValueStatement(io.Right, sb, cmd, info, io.Left as FieldStatement, context);
            sb.Append(") ");
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
            sb.Append(' ');
            BuildValueStatement(os.Left, sb, cmd, info, os.Left as FieldStatement, context);
            sb.Append(' ');
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
                    var rf = os.Right as StringValueStatement;
                    sb.Append($"'%{ReplaceLikeValue(rf.Value)}%'");
                    break;

                case "prefix-like":
                    sb.Append("like ");
                    var lrf = os.Right as StringValueStatement;
                    sb.Append($"'{ReplaceLikeValue(lrf.Value)}%'");
                    break;

                case "suffix-like":
                    sb.Append("like ");
                    var srf = os.Right as StringValueStatement;
                    sb.Append($"'%{ReplaceLikeValue(srf.Value)}'");
                    break;

                default:
                    sb.Append(os.Operater);
                    sb.Append(' ');
                    BuildValueStatement(os.Right, sb, cmd, info, os.Right as FieldStatement, context);
                    break;
            }

            sb.Append(' ');
        }

        private static void BuildValueStatement(ValueStatement v, StringBuilder sb, DbCommand cmd, DbEntityInfo info, FieldStatement? fieldValueStatement, BuildConditionContext context)
        {
            if (ConvertField(v, info, sb, false))
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
using Microsoft.Data.Sqlite;
using SV.Db.Sloth.Statements;
using System.Data;
using System.Data.Common;
using System.Text;

namespace SV.Db.Sloth.SQLite
{
    public class SQLiteConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new SqliteConnection(connectionString);
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
            var fs = statement.Fields?.Fields;
            var sql = new StringBuilder();

            var hasTotal = fs?.FirstOrDefault(i => i is FuncCallerStatement f && f.Name.Equals("count()", StringComparison.OrdinalIgnoreCase));
            if (hasTotal != null)
            {
                fs.Remove(hasTotal);
                sql.Append("SELECT count(*) FROM {{TableTotal}} ; ");
                hasTotalCount = true;
            }
            else
            {
                hasTotalCount = false;
            }

            if (fs.IsNotNullOrEmpty())
            {
                hasRows = true;
                sql.Append("SELECT {{Fields}} FROM {{Table}} ");
            }
            else
            {
                hasRows = false;
            }

            var table = info.Table;
            if (table.Contains("{{Where}}", StringComparison.OrdinalIgnoreCase))
            {
                table = table.Replace("{{Where}}", "{{Where}}", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                table += " {{Where}} ";
            }

            if (table.Contains("{{OrderBy}}", StringComparison.OrdinalIgnoreCase))
            {
                table = table.Replace("{{OrderBy}}", "{{OrderBy}}", StringComparison.OrdinalIgnoreCase);
                sql = sql.Replace("{{TableTotal}}", table.Replace("{{OrderBy}}", string.Empty));
                if (hasRows)
                {
                    sql = sql.Replace("{{Table}}", table);
                }
            }
            else
            {
                sql = sql.Replace("{{TableTotal}}", table);
                if (hasRows)
                {
                    sql = sql.Replace("{{Table}}", table);
                    sql = sql.Append(" {{OrderBy}}");
                }
            }

            if (fs?.Any(i => i is FieldStatement f && f.Name.Equals("*", StringComparison.OrdinalIgnoreCase)) == true)
            {
                sql = sql.Replace("{{Fields}}", string.Join(",", info.SelectFields.Select(i => i.Value)));
            }
            else if (hasRows)
            {
                sql = sql.Replace("{{Fields}}", string.Join(",", fs.Select(i => info.SelectFields.TryGetValue(i.Name, out var v) ? v : null).Where(i => !string.IsNullOrWhiteSpace(i))));
            }

            if (statement.Where == null || statement.Where.Condition == null)
            {
                sql = sql.Replace("{{Where}}", string.Empty);
            }
            else
            {
                sql = sql.Replace("{{Where}}", BuildCondition(cmd, info, statement.Where.Condition));
            }

            if (statement.OrderBy == null || statement.OrderBy.Fields.IsNullOrEmpty())
            {
                sql = sql.Replace("{{OrderBy}}", " {{Limit}} ");
            }
            else
            {
                sql = sql.Replace("{{OrderBy}}", " order by " + string.Join(",", statement.OrderBy.Fields.Select(i => $"{i.Name} {(i.Direction == OrderByDirection.Asc ? "asc" : "desc")}")) + " {{Limit}} ");
            }

            if (statement.Limit == null)
            {
                statement.Limit = new LimitStatement() { Rows = 10 };
            }
            if (!statement.Limit.Offset.HasValue)
            {
                statement.Limit.Offset = 0;
            }
            sql = sql.Replace("{{Limit}}", $"Limit {statement.Limit.Offset},{statement.Limit.Rows} ");

            cmd.CommandText = sql.ToString();
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
        }

        private static void BuildOperaterStatement(StringBuilder sb, OperaterStatement os, DbCommand cmd, DbEntityInfo info, BuildConditionContext context)
        {
            sb.Append(' ');
            BuildValueStatement(os.Left, sb, cmd, info, os.Left as FieldValueStatement, context);
            sb.Append(' ');
            sb.Append(os.Operater);
            sb.Append(' ');
            BuildValueStatement(os.Right, sb, cmd, info, os.Right as FieldValueStatement, context);
            sb.Append(' ');
        }

        private static void BuildValueStatement(ValueStatement v, StringBuilder sb, DbCommand cmd, DbEntityInfo info, FieldValueStatement? fieldValueStatement, BuildConditionContext context)
        {
            if (v is FieldValueStatement f)
            {
                sb.Append(info.SelectFields[f.Field]);
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
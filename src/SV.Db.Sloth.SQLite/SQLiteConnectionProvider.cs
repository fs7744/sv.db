using Microsoft.Data.Sqlite;
using SV.Db.Sloth.Statements;
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
            throw new NotImplementedException();
        }

        public async Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            var hasTotal = BuildSelectStatement(cmd, info, statement);
            using var reader = await CommandExtensions.DbDataReaderAsync(cmd, System.Data.CommandBehavior.CloseConnection, cancellationToken);
            var result = new PageResult<T>();
            if (hasTotal)
            {
                result.TotalCount = await reader.QueryFirstOrDefaultAsync<int>();
            }
            result.Rows = await reader.QueryAsync<T>(cancellationToken).ToListAsync(cancellationToken);
            return result;
        }

        private bool BuildSelectStatement(DbCommand cmd, DbEntityInfo info, SelectStatement statement)
        {
            var sql = new StringBuilder("SELECT {{Fields}} FROM {{Table}} ");
            var hasTotal = statement.Fields.Fields.Any(i => i is FuncCallerStatement f && f.Name.Equals("count()", StringComparison.OrdinalIgnoreCase));
            if (hasTotal)
            {
                sql.Insert(0, "SELECT count(*) FROM {{TableTotal}} ; ");
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
                sql = sql.Replace("{{Table}}", table);
            }
            else
            {
                sql = sql.Replace("{{TableTotal}}", table);
                sql = sql.Replace("{{Table}}", table);
                sql = sql.Append(" {{OrderBy}}");
            }

            if (statement.Fields.Fields.Any(i => i is FieldStatement f && f.Name.Equals("*", StringComparison.OrdinalIgnoreCase)))
            {
                sql = sql.Replace("{{Fields}}", string.Join(",", info.SelectFields.Select(i => i.Value)));
            }
            else
            {
                sql = sql.Replace("{{Fields}}", string.Join(",", statement.Fields.Fields.Select(i => info.SelectFields.TryGetValue(i.Name, out var v) ? v : null).Where(i => !string.IsNullOrWhiteSpace(i))));
            }

            if (statement.Where == null || statement.Where.Condition == null)
            {
                sql = sql.Replace("{{Where}}", string.Empty);
            }
            else
            {
                sql = sql.Replace("{{Where}}", string.Empty);
                //todo
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
            return hasTotal;
        }
    }
}
using Microsoft.Data.Sqlite;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db.Sloth.SQLite
{
    public class SQLiteConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public PageResult<T> ExecuteQuery<T>(string connectionString, SelectStatement statement)
        {
            throw new NotImplementedException();
        }

        public async Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            using var connection = Create(connectionString);
            var cmd = connection.CreateCommand();
            var hasTotal = BuildSelectStatement(cmd, statement);
            using var reader = await CommandExtensions.DbDataReaderAsync(cmd, System.Data.CommandBehavior.CloseConnection, cancellationToken);
            var result = new PageResult<T>();
            if (hasTotal)
            {
                result.TotalCount = await reader.QueryFirstOrDefaultAsync<int>();
            }
            result.Rows = await reader.QueryAsync<T>(cancellationToken).ToListAsync(cancellationToken);
            return result;
        }

        private bool BuildSelectStatement(DbCommand cmd, SelectStatement statement)
        {
            // todo
            cmd.CommandText = """
    SELECT count(1)
    FROM Weather;
    SELECT *
    FROM Weather;
    """;
            return true;
        }
    }
}
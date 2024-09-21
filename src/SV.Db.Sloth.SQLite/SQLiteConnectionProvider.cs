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

        public Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, SelectStatement statement)
        {
            throw new NotImplementedException();
        }
    }
}
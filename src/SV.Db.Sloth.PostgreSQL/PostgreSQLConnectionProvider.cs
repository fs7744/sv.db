using Npgsql;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db.Sloth.MySql
{
    public class PostgreSQLConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
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
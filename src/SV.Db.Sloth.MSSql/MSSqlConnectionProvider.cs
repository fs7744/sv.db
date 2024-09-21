using Microsoft.Data.SqlClient;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db.Sloth.MySql
{
    public class MSSqlConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public PageResult<T> ExecuteQuery<T>(string connectionString, SelectStatement statement)
        {
            throw new NotImplementedException();
        }

        public Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, SelectStatement statement, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
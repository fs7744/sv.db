using Npgsql;
using System.Data.Common;

namespace SV.Db.Sloth.MySql
{
    public class PostgreSQLConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
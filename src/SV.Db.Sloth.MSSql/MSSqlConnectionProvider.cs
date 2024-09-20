using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace SV.Db.Sloth.MySql
{
    public class MSSqlConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
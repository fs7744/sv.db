using MySqlConnector;
using System.Data.Common;

namespace SV.Db.Sloth.MySql
{
    public class MySqlConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }
    }
}
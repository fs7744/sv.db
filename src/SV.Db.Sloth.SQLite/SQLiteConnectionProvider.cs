using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace SV.Db.Sloth.SQLite
{
    public class SQLiteConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }
    }
}
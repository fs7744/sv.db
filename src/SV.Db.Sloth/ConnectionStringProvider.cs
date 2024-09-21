using SV.Db.Sloth;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db
{
    public abstract class ConnectionStringProvider : IConnectionStringProvider
    {
        public const string SQLite = nameof(SQLite);
        public const string PostgreSQL = nameof(PostgreSQL);
        public const string MySql = nameof(MySql);
        public const string MSSql = nameof(MSSql);

        public abstract bool ContainsKey(string key);

        public PageResult<T> ExecuteQuery<T>(string key, SelectStatement statement)
        {
            (string dbType, string connectionString) = Get(key);
            var p = ConnectionFactory.GetProvider(dbType);
            return p.ExecuteQuery<T>(connectionString, statement);
        }

        public Task<PageResult<T>> ExecuteQueryAsync<T>(string key, SelectStatement statement)
        {
            (string dbType, string connectionString) = Get(key);
            var p = ConnectionFactory.GetProvider(dbType);
            return p.ExecuteQueryAsync<T>(connectionString, statement);
        }

        public abstract (string dbType, string connectionString) Get(string key);

        public DbConnection GetConnection(string key)
        {
            (string dbType, string connectionString) = Get(key);
            return ConnectionFactory.Get(dbType, connectionString);
        }
    }
}
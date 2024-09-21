using System.Data.Common;

namespace SV.Db
{
    public abstract class ConnectionStringProvider : IConnectionStringProvider, IConnectionFactory
    {
        public const string SQLite = nameof(SQLite);
        public const string PostgreSQL = nameof(PostgreSQL);
        public const string MySql = nameof(MySql);
        public const string MSSql = nameof(MSSql);

        public abstract bool ContainsKey(string key);

        public abstract (string dbType, string connectionString) Get(string key);

        public DbConnection GetConnection(string key)
        {
            (string dbType, string connectionString) = Get(key);
            return ConnectionFactory.Get(dbType, connectionString);
        }
    }

    public interface IConnectionFactory
    {
        DbConnection GetConnection(string key);
    }

    public class ConnectionStringProviders : ConnectionStringProvider
    {
        private readonly IConnectionStringProvider[] providers;

        public ConnectionStringProviders(IConnectionStringProvider[] providers)
        {
            this.providers = providers;
        }

        public override bool ContainsKey(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return true;
            }
            return false;
        }

        public override (string dbType, string connectionString) Get(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return item.Get(key);
            }
            throw new KeyNotFoundException(key);
        }
    }
}
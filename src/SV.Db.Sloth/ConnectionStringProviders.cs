using SV.Db.Sloth.Attributes;
using System.Collections.Frozen;
using System.Reflection;

namespace SV.Db
{
    public class ConnectionStringProviders : ConnectionStringProvider, IConnectionFactory
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

        public DbEntityInfo GetDbEntityInfo<T>()
        {
            return DbEntityInfo<T>.Get();
        }
    }
}
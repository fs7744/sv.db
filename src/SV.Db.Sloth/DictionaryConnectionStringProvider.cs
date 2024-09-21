using System.Collections.Concurrent;

namespace SV.Db
{
    public class DictionaryConnectionStringProvider : ConnectionStringProvider
    {
        public static readonly DictionaryConnectionStringProvider Instance = new DictionaryConnectionStringProvider();

        public readonly ConcurrentDictionary<string, (string dbType, string connectionString)> Cache = new ConcurrentDictionary<string, (string dbType, string connectionString)>(StringComparer.OrdinalIgnoreCase);

        public override (string dbType, string connectionString) Get(string key)
        {
            if (!Cache.TryGetValue(key, out var value))
                throw new KeyNotFoundException(key);
            return value;
        }

        public void Add(string key, (string dbType, string connectionString) value)
        {
            Cache[key] = value;
        }

        public void Add(string key, string dbType, string connectionString)
        {
            Add(key, (dbType, connectionString));
        }

        public override bool ContainsKey(string key)
        {
            return Cache.ContainsKey(key);
        }
    }
}
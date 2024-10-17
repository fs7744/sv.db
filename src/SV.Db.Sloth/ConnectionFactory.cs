using System.Collections.Concurrent;
using System.Data.Common;

namespace SV.Db
{
    public static class ConnectionFactory
    {
        private static readonly ConcurrentDictionary<string, IConnectionProvider> connectionProviders = new ConcurrentDictionary<string, IConnectionProvider>(StringComparer.OrdinalIgnoreCase);
        internal static Func<string, object> JsonTokenParse = s => System.Text.Json.JsonDocument.Parse(s);

        public static void RegisterConnectionProvider(string type, IConnectionProvider connectionProvider)
        {
            connectionProviders[type] = connectionProvider;
        }

        public static bool HasType(string type)
        {
            return connectionProviders.ContainsKey(type);
        }

        public static IConnectionProvider GetProvider(string type)
        {
            if (!connectionProviders.TryGetValue(type, out var conn))
                throw new KeyNotFoundException(type);
            return conn;
        }

        public static DbConnection Get(string type, string connectionString)
        {
            if (!connectionProviders.TryGetValue(type, out var conn))
                throw new KeyNotFoundException(type);
            return TransactionConnectionFactory.GetOrAdd(connectionString, conn.Create);
        }

        public static void ReplaceJsonTokenParse(Func<string, object> func)
        {
            if (func != null)
                JsonTokenParse = func;
        }

        public static object ParseJsonToken(string str)
        {
            return JsonTokenParse(str);
        }
    }
}
﻿namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static Task<int> ExecuteInsertAsync<T>(this IConnectionFactory factory, T data, CancellationToken cancellationToken = default)
        {
            var info = factory.GetDbEntityInfoOfT<T>();
            (string dbType, string connectionString) = factory.Get(info.DbKey);
            var p = ConnectionFactory.GetProvider(dbType);
            return p.ExecuteInsertAsync<T>(p.Create(connectionString), info, data, cancellationToken);
        }

        public static Task<int> ExecuteInsertAsync<T>(this IConnectionFactory factory, IEnumerable<T> data, int batchSize = 100, CancellationToken cancellationToken = default)
        {
            var info = factory.GetDbEntityInfoOfT<T>();
            (string dbType, string connectionString) = factory.Get(info.DbKey);
            var p = ConnectionFactory.GetProvider(dbType);
            return p.ExecuteInsertAsync<T>(p.Create(connectionString), info, data, batchSize, cancellationToken);
        }

        public static Task<int> ExecuteUpdateAsync<T>(this IConnectionFactory factory, T data, CancellationToken cancellationToken = default)
        {
            var info = factory.GetDbEntityInfoOfT<T>();
            (string dbType, string connectionString) = factory.Get(info.DbKey);
            var p = ConnectionFactory.GetProvider(dbType);
            return p.ExecuteUpdateAsync<T>(p.Create(connectionString), info, data, cancellationToken);
        }

        public static async Task<int> ExecuteUpdateAsync<T>(this IConnectionFactory factory, IEnumerable<T> data, CancellationToken cancellationToken = default)
        {
            var info = factory.GetDbEntityInfoOfT<T>();
            (string dbType, string connectionString) = factory.Get(info.DbKey);
            var p = ConnectionFactory.GetProvider(dbType);
            var total = 0;
            var connection = p.Create(connectionString);
            foreach (var item in data)
            {
                total += await p.ExecuteUpdateAsync<T>(connection, info, item, cancellationToken);
            }
            return total;
        }
    }
}
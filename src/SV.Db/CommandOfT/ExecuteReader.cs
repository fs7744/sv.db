using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static DbDataReader ExecuteReader(this DbCommand cmd, object? args = null, CommandBehavior behavior = CommandBehavior.Default)
        {
            cmd.SetParams(args);
            var connection = cmd.Connection;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            return cmd.ExecuteReader(behavior);
        }

        public static async Task<DbDataReader> ExecuteReaderAsync(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.CloseConnection)
        {
            cmd.SetParams(args);
            var connection = cmd.Connection;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
            return await cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static DbDataReader ExecuteReader(this DbConnection connection, string sql, object? args = null, CommandBehavior behavior = CommandBehavior.CloseConnection, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            return cmd.ExecuteReader(behavior);
        }

        public static async Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.CloseConnection, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
            return await cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static T? QueryFirstOrDefault<T>(this DbDataReader reader)
        {
            if (reader.HasRows)
            {
                var r = reader.Read<T>();
                reader.NextResult();
                return r;
            }
            return default;
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<DbDataReader> DbDataReaderAsync(DbCommand cmd, CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            var connection = cmd.Connection;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
            return await cmd.ExecuteReaderAsync(behavior, cancellationToken); ;
        }

        [MethodImpl(DBUtils.Optimization)]
        public static T? DbDataReaderQueryFirstOrDefault<T>(this IRecordFactory<T> factory, DbDataReader reader)
        {
            if (reader.HasRows)
            {
                var r = factory.Read(reader);
                if (reader.HasRows && !reader.IsClosed)
                {
                    reader.NextResult();
                }
                return r;
            }
            return default;
        }

        public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbDataReader reader, CancellationToken cancellationToken = default)
        {
            if (reader.HasRows)
            {
                var r = reader.Read<T>();
                if (reader.HasRows && !reader.IsClosed)
                {
                    await reader.NextResultAsync(cancellationToken);
                }
                return r;
            }
            return default;
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<T?> DbDataReaderQueryFirstOrDefaultAsync<T>(this IRecordFactory<T> factory, DbDataReader reader, CancellationToken cancellationToken = default)
        {
            if (reader.HasRows)
            {
                var r = factory.Read(reader);
                if (reader.HasRows && !reader.IsClosed)
                {
                    await reader.NextResultAsync(cancellationToken);
                }
                return r;
            }
            return default;
        }

        public static IEnumerable<T?> Query<T>(this DbDataReader reader, int estimateRow = 0)
        {
            if (reader.HasRows)
            {
                var r = reader.ReadEnumerable<T>(estimateRow, true);
                if (reader.HasRows && !reader.IsClosed)
                {
                    reader.NextResult();
                }
                return r;
            }
            return Enumerable.Empty<T>();
        }

        [MethodImpl(DBUtils.Optimization)]
        public static IEnumerable<T?> DbDataReaderQuery<T>(this IRecordFactory<T> factory, DbDataReader reader, int estimateRow = 0)
        {
            if (reader.HasRows)
            {
                var r = factory.ReadBuffed(reader, estimateRow);
                if (reader.HasRows && !reader.IsClosed)
                {
                    reader.NextResult();
                }
                return r;
            }
            return Enumerable.Empty<T>();
        }

        public static async IAsyncEnumerable<T> QueryAsync<T>(this DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (reader.HasRows)
            {
                await foreach (var item in reader.ReadEnumerableAsync<T>(cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                if (reader.HasRows && !reader.IsClosed)
                {
                    await reader.NextResultAsync(cancellationToken);
                }
            }
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async IAsyncEnumerable<T> DbDataReaderQueryAsync<T>(this IRecordFactory<T> factory, DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (reader.HasRows)
            {
                await foreach (var item in factory.ReadUnBuffedAsync(reader, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                if (reader.HasRows && !reader.IsClosed)
                {
                    await reader.NextResultAsync(cancellationToken);
                }
            }
        }
    }
}
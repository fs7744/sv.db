using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static DbDataReader ExecuteReader(this DbCommand cmd, object args, CommandBehavior behavior = CommandBehavior.Default)
        {
            cmd.SetParams(args);
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbCommand cmd, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            cmd.SetParams(args);
            return cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static DbDataReader ExecuteReader(this DbConnection connection, string sql, object args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static T? QueryFirstOrDefault<T>(this DbDataReader reader)
        {
            var r = reader.Read<T>();
            reader.NextResult();
            return r;
        }

        public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbDataReader reader, CancellationToken cancellationToken = default)
        {
            var r = reader.Read<T>();
            await reader.NextResultAsync(cancellationToken);
            return r;
        }

        public static IEnumerable<T> Query<T>(this DbDataReader reader, int estimateRow = 0, bool useBuffer = true)
        {
            var r = reader.ReadEnumerable<T>(estimateRow, useBuffer);
            reader.NextResult();
            return r;
        }

        public static async Task<IAsyncEnumerable<T>> QueryAsync<T>(this DbDataReader reader, CancellationToken cancellationToken = default)
        {
            var r = reader.ReadEnumerableAsync<T>(cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            return r;
        }
    }
}
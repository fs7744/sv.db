using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static IEnumerable<T> ExecuteQuery<T>(this DbCommand command, object args = null, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0, bool useBuffer = true)
        {
            if (args != null)
            {
                // todo: params
            }
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = command.ExecuteReader(behavior))
                {
                    var r = reader.ReadEnumerable<T>(estimateRow, useBuffer);
                    while (reader.NextResult()) { }
                    return r;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public static async Task<IAsyncEnumerable<T>> ExecuteQueryAsync<T>(this DbCommand command, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            if (args != null)
            {
                // todo: params
            }
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    var r = reader.ReadEnumerableAsync<T>(cancellationToken);
                    while (await reader.NextResultAsync(cancellationToken)) { }
                    return r;
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static Task<IAsyncEnumerable<T>> ExecuteQueryAsync<T>(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return ExecuteQueryAsync<T>(cmd, args, cancellationToken, behavior);
        }

        public static IEnumerable<T> ExecuteQuery<T>(this DbConnection connection, string sql, object args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text, int estimateRow = 0, bool useBuffer = true)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return ExecuteQuery<T>(cmd, args, behavior, estimateRow, useBuffer);
        }

        public static T? ExecuteQueryFirstOrDefault<T>(this DbCommand command, object args = null, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            if (args != null)
            {
                // todo: params
            }
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = command.ExecuteReader(behavior))
                {
                    var r = reader.ReadEnumerable<T>(0, false).FirstOrDefault();
                    while (reader.NextResult()) { }
                    return r;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public static T? ExecuteQueryFirstOrDefault<T>(this DbConnection connection, string sql, object args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text, int estimateRow = 0, bool useBuffer = true)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return ExecuteQueryFirstOrDefault<T>(cmd, args, behavior);
        }

        public static async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbCommand command, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            if (args != null)
            {
                // todo: params
            }
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    var rr = reader.ReadEnumerableAsync<T>(cancellationToken).GetAsyncEnumerator(cancellationToken);
                    var r = await rr.MoveNextAsync() ? rr.Current : default;
                    while (await reader.NextResultAsync(cancellationToken)) { }
                    await rr.DisposeAsync();
                    return r;
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return ExecuteQueryFirstOrDefaultAsync<T>(cmd, args, cancellationToken, behavior);
        }
    }
}
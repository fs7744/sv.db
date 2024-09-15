using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static IEnumerable<T?> ExecuteQuery<T>(this DbCommand command, object? args = null, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0)
        {
            command.SetParams(args);
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = command.ExecuteReader(behavior))
                {
                    var r = reader.ReadEnumerable<T>(estimateRow, true);
                    while (reader.NextResult()) { }
                    return r;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public static async IAsyncEnumerable<T?> ExecuteQueryAsync<T>(this DbCommand command, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            command.SetParams(args);
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    await foreach (var item in reader.ReadEnumerableAsync<T>(cancellationToken).WithCancellation(cancellationToken))
                    {
                        yield return item;
                    }
                    while (await reader.NextResultAsync(cancellationToken)) { }
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static IAsyncEnumerable<T?> ExecuteQueryAsync<T>(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return ExecuteQueryAsync<T>(cmd, args, cancellationToken, behavior);
        }

        public static IEnumerable<T?> ExecuteQuery<T>(this DbConnection connection, string sql, object? args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text, int estimateRow = 0)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return ExecuteQuery<T>(cmd, args, behavior, estimateRow);
        }

        public static T? ExecuteQueryFirstOrDefault<T>(this DbCommand command, object? args = null, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            command.SetParams(args);
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

        public static T? ExecuteQueryFirstOrDefault<T>(this DbConnection connection, string sql, object? args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text, int estimateRow = 0, bool useBuffer = true)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return ExecuteQueryFirstOrDefault<T>(cmd, args, behavior);
        }

        public static async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbCommand command, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            command.SetParams(args);
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

        public static Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return ExecuteQueryFirstOrDefaultAsync<T>(cmd, args, cancellationToken, behavior);
        }
    }
}
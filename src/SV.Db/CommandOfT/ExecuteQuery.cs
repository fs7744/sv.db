using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static IEnumerable<T?> ExecuteQuery<T>(this DbCommand cmd, object? args = null, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0)
        {
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQuery(factory, cmd, behavior, estimateRow);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static IEnumerable<T?> DbCommandExecuteQuery<T>(this IRecordFactory<T> factory, DbCommand cmd, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0)
        {
            var connection = cmd.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = cmd.ExecuteReader(behavior))
                {
                    var r = factory.ReadBuffed(reader, estimateRow);
                    while (reader.NextResult()) { }
                    return r;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public static IAsyncEnumerable<T?> ExecuteQueryAsync<T>(this DbCommand cmd, object? args = null, [EnumeratorCancellation] CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryAsync(factory, cmd, cancellationToken, behavior);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async IAsyncEnumerable<T?> DbCommandExecuteQueryAsync<T>(this IRecordFactory<T> factory, DbCommand command, [EnumeratorCancellation] CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    await foreach (var item in factory.ReadUnBuffedAsync(reader, cancellationToken).WithCancellation(cancellationToken))
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
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryAsync(factory, cmd, cancellationToken, behavior);
        }

        public static IEnumerable<T?> ExecuteQuery<T>(this DbConnection connection, string sql, object? args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text, int estimateRow = 0)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQuery(factory, cmd, behavior, estimateRow);
        }

        public static T? ExecuteQueryFirstOrDefault<T>(this DbCommand cmd, object? args = null, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryFirstOrDefault(factory, cmd, behavior);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static T? DbCommandExecuteQueryFirstOrDefault<T>(this IRecordFactory<T> factory, DbCommand command, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = command.ExecuteReader(behavior))
                {
                    var r = factory.ReadUnBuffed(reader).FirstOrDefault();
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
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryFirstOrDefault(factory, cmd, behavior);
        }

        public static Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryFirstOrDefaultAsync<T>(factory, cmd, cancellationToken, behavior);
        }

        public static Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            var factory = RecordFactory.GetRecordFactory<T>();
            return DbCommandExecuteQueryFirstOrDefaultAsync<T>(factory, cmd, cancellationToken, behavior);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<T?> DbCommandExecuteQueryFirstOrDefaultAsync<T>(this IRecordFactory<T> factory, DbCommand command, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.SingleRow)
        {
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    var rr = factory.ReadUnBuffedAsync(reader, cancellationToken).GetAsyncEnumerator(cancellationToken);
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
    }
}
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuerys<T>(this DbConnection connection, string sql, List<T> args, int batchSize = 100, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerys<T>(factory, connection, sql, args, batchSize, commandType);
        }

        public static int ExecuteNonQuerys<T>(this DbConnection connection, string sql, T[] args, int batchSize = 100, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerys(factory, connection, sql, args, batchSize, commandType);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static int DbConnectionExecuteNonQuerys<T>(this IParamsSetter<T> factory , DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CommandType commandType = CommandType.Text)
        {
            if (args == null)
            {
                return 0;
            }
            else if (args.TryGetNonEnumeratedCount(out var count))
            {
                if (count == 0) return 0;
                if (count == 1) return connection.ExecuteNonQuery(sql, args.First(), commandType);
            }
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                var total = 0;
                foreach (var item in args.Page(batchSize))
                {
                    var batch = connection.CreateBatch();
                    foreach (var i in item)
                    {
                        var cmd = batch.CreateBatchCommand();
                        cmd.CommandText = sql;
                        cmd.CommandType = commandType;
                        factory.SetParams(cmd, i);
                        batch.BatchCommands.Add(cmd);
                    }
                    total += batch.ExecuteNonQuery();
                }
                return total;
            }
            finally
            {
                connection.Close();
            }
        }

        public static int ExecuteNonQuerys<T>(this DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerys<T>(factory, connection, sql, args, batchSize, commandType);
        }

        public static Task<int> ExecuteNonQuerysAsync<T>(this DbConnection connection, string sql, List<T> args, int batchSize = 100, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerysAsync(factory, connection, sql, args, batchSize, cancellationToken, commandType);
        }

        public static Task<int> ExecuteNonQuerysAsync<T>(this DbConnection connection, string sql, T[] args, int batchSize = 100, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerysAsync(factory, connection, sql, args, batchSize, cancellationToken, commandType);
        }

        public static Task<int> ExecuteNonQuerysAsync<T>(this DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var factory = RecordFactory.GetParamsSetter<T>();
            return DbConnectionExecuteNonQuerysAsync(factory, connection, sql, args, batchSize, cancellationToken, commandType);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<int> DbConnectionExecuteNonQuerysAsync<T>(this IParamsSetter<T> factory, DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            if (args == null)
            {
                return 0;
            }
            else if (args.TryGetNonEnumeratedCount(out var count))
            {
                if (count == 0) return 0;
                if (count == 1) return await connection.ExecuteNonQueryAsync(sql, args.First(), cancellationToken, commandType);
            }
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                var total = 0;
                foreach (var item in args.Page(batchSize))
                {
                    var batch = connection.CreateBatch();
                    foreach (var i in item)
                    {
                        var cmd = batch.CreateBatchCommand();
                        cmd.CommandText = sql;
                        cmd.CommandType = commandType;
                        factory.SetParams(cmd, i);
                        batch.BatchCommands.Add(cmd);
                    }
                    total += await batch.ExecuteNonQueryAsync(cancellationToken);
                }
                return total;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
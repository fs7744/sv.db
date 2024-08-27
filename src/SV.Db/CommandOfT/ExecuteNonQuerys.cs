using System.Data;
using System.Data.Common;
using System.Threading;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuerys<T>(this DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CommandType commandType = CommandType.Text)
        {
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
                        cmd.SetParams<T>(i);
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

        public static async Task<int> ExecuteNonQuerysAsync<T>(this DbConnection connection, string sql, IEnumerable<T> args, int batchSize = 100, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
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
                        cmd.SetParams<T>(i);
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
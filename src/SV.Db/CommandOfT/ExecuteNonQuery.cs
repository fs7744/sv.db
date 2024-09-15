using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuery(this DbCommand cmd, object? args = null)
        {
            cmd.SetParams(args);
            return DbCommandExecuteNonQuery(cmd);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static int DbCommandExecuteNonQuery(DbCommand cmd)
        {
            var connection = cmd.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public static Task<int> ExecuteNonQueryAsync(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default)
        {
            cmd.SetParams(args);
            return DbCommandExecuteNonQueryAsync(cmd, cancellationToken);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<int> DbCommandExecuteNonQueryAsync(DbCommand cmd, CancellationToken cancellationToken = default)
        {
            var connection = cmd.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static int ExecuteNonQuery(this DbConnection connection, string sql, object? args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return DbCommandExecuteNonQuery(cmd);
        }

        public static Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return DbCommandExecuteNonQueryAsync(cmd, cancellationToken);
        }
    }
}
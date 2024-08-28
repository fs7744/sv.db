using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuery(this DbCommand cmd, object? args = null)
        {
            cmd.SetParams(args);
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

        public static async Task<int> ExecuteNonQueryAsync(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default)
        {
            cmd.SetParams(args);
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
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandType = commandType;
                cmd.SetParams(args);
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
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
    }
}
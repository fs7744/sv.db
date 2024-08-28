using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static T ExecuteScalar<T>(this DbCommand command, object? args = null)
        {
            command.SetParams(args);
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                return DBUtils.As<T>(command.ExecuteScalar());
            }
            finally
            {
                connection.Close();
            }
        }

        public static async Task<T> ExecuteScalarAsync<T>(this DbCommand command, object? args = null, CancellationToken cancellationToken = default)
        {
            command.SetParams(args);
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                return DBUtils.As<T>(await command.ExecuteScalarAsync(cancellationToken));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static object? ExecuteScalar(this DbConnection connection, string sql, object? args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                return cmd.ExecuteScalar();
            }
            finally
            {
                connection.Close();
            }
        }

        public static T ExecuteScalar<T>(this DbConnection connection, string sql, object? args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalar<T>();
        }

        public static async Task<object?> ExecuteScalarAsync(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
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
                return await cmd.ExecuteScalarAsync(cancellationToken);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static Task<T> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalarAsync<T>(cancellationToken);
        }
    }
}
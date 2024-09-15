using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static object? ExecuteScalar(this DbCommand cmd, object? args = null)
        {
            cmd.SetParams(args);
            return DbCommandExecuteScalarObject(cmd);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static object? DbCommandExecuteScalarObject(DbCommand cmd)
        {
            var connection = cmd.Connection;
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

        public static T ExecuteScalar<T>(this DbCommand cmd, object? args = null)
        {
            cmd.SetParams(args);
            return DbCommandExecuteScalar<T>(cmd);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static T DbCommandExecuteScalar<T>(DbCommand cmd)
        {
            var connection = cmd.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                return DBUtils.As<T>(cmd.ExecuteScalar());
            }
            finally
            {
                connection.Close();
            }
        }

        public static Task<object?> ExecuteScalarAsync(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default)
        {
            cmd.SetParams(args);
            return DbCommandExecuteScalarObjectAsync(cmd, cancellationToken);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<object?> DbCommandExecuteScalarObjectAsync(DbCommand cmd, CancellationToken cancellationToken = default)
        {
            var connection = cmd.Connection;
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

        public static Task<T> ExecuteScalarAsync<T>(this DbCommand cmd, object? args = null, CancellationToken cancellationToken = default)
        {
            cmd.SetParams(args);
            return DbCommandExecuteScalarAsync<T>(cmd, cancellationToken);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static async Task<T> DbCommandExecuteScalarAsync<T>(DbCommand cmd, CancellationToken cancellationToken = default)
        {
            var connection = cmd.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                return DBUtils.As<T>(await cmd.ExecuteScalarAsync(cancellationToken));
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
            return DbCommandExecuteScalar(cmd);
        }

        [MethodImpl(DBUtils.Optimization)]
        public static object? DbCommandExecuteScalar(DbCommand cmd)
        {
            var connection = cmd.Connection;
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

        public static Task<object?> ExecuteScalarAsync(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return DbCommandExecuteScalarObjectAsync(cmd, cancellationToken);
        }

        public static Task<T> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object? args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return DbCommandExecuteScalarAsync<T>(cmd, cancellationToken);
        }
    }
}
using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static T ExecuteScalar<T>(this DbCommand command, object args = null)
        {
            command.SetParams(args);
            return DBUtils.As<T>(command.ExecuteScalar());
        }

        public static async Task<T> ExecuteScalarAsync<T>(this DbCommand command, object args = null, CancellationToken cancellationToken = default)
        {
            command.SetParams(args);
            return DBUtils.As<T>(await command.ExecuteScalarAsync(cancellationToken));
        }

        public static object? ExecuteScalar(this DbConnection connection, string sql, object args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalar();
        }

        public static T ExecuteScalar<T>(this DbConnection connection, string sql, object args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalar<T>();
        }

        public static Task<object?> ExecuteScalarAsync(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalarAsync(cancellationToken);
        }

        public static Task<T> ExecuteScalarAsync<T>(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteScalarAsync<T>(cancellationToken);
        }
    }
}
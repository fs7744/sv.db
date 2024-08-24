using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static T ExecuteScalar<T>(this DbCommand command)
        {
            return DBUtils.As<T>(command.ExecuteScalar());
        }

        public static T ExecuteScalar<T>(this DbCommand command, object args)
        {
            //todo: params
            return DBUtils.As<T>(command.ExecuteScalar());
        }

        public static object? ExecuteScalar(this DbConnection connection, string sql, object args, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteScalar();
        }

        public static T ExecuteScalar<T>(this DbConnection connection, string sql, object args, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteScalar<T>();
        }

        public static T ExecuteScalarAsync<T>(this DbCommand command)
        {
            return command.ExecuteScalarAsync<T>(CancellationToken.None);
        }

        public static T ExecuteScalarAsync<T>(this DbCommand command, CancellationToken cancellationToken)
        {
            return DBUtils.As<T>(command.ExecuteScalarAsync(cancellationToken));
        }

        public static T ExecuteScalarAsync<T>(this DbCommand command, object args, CancellationToken cancellationToken)
        {
            //todo: params
            return command.ExecuteScalarAsync<T>(cancellationToken);
        }

        public static object? ExecuteScalarAsync(this DbConnection connection, string sql, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteScalarAsync(sql, CancellationToken.None, commandType);
        }

        public static object? ExecuteScalarAsync(this DbConnection connection, string sql, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteScalarAsync();
        }

        public static object? ExecuteScalarAsync(this DbConnection connection, string sql, object args, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteScalarAsync(sql, args, CancellationToken.None, commandType);
        }

        public static object? ExecuteScalarAsync(this DbConnection connection, string sql, object args, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteScalarAsync();
        }

        public static T ExecuteScalarAsync<T>(this DbConnection connection, string sql, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteScalarAsync<T>(cancellationToken);
        }

        public static T ExecuteScalarAsync<T>(this DbConnection connection, string sql, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteScalarAsync<T>(sql, CancellationToken.None, commandType);
        }

        public static T ExecuteScalarAsync<T>(this DbConnection connection, string sql, object args, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteScalarAsync<T>(sql, args, CancellationToken.None, commandType);
        }

        public static T ExecuteScalarAsync<T>(this DbConnection connection, string sql, object args, CancellationToken cancellationToken, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteScalarAsync<T>(cancellationToken);
        }
    }
}
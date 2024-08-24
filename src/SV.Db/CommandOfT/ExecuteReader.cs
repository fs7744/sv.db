using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static DbDataReader ExecuteReader(this DbConnection connection, string sql, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteReaderAsync(sql, CancellationToken.None, behavior, commandType);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, CancellationToken cancellationToken, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteReaderAsync(cancellationToken, behavior);
        }

        public static DbDataReader ExecuteReader(this DbConnection connection, string sql, object args, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteReader(args, behavior);
        }

        public static DbDataReader ExecuteReader(this DbCommand cmd, object args, CommandBehavior behavior = CommandBehavior.Default)
        {
            //todo: params
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, object args, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            return connection.ExecuteReaderAsync(sql, args, CancellationToken.None, behavior, commandType);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, object args, CancellationToken cancellationToken, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            return cmd.ExecuteReaderAsync(args, cancellationToken, behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbCommand cmd, object args, CancellationToken cancellationToken, CommandBehavior behavior = CommandBehavior.Default)
        {
            //todo: params
            return cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbCommand cmd, object args, CommandBehavior behavior = CommandBehavior.Default)
        {
            return cmd.ExecuteReaderAsync(args, CancellationToken.None, behavior);
        }
    }
}
using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static DbDataReader ExecuteReader(this DbCommand cmd, object args, CommandBehavior behavior = CommandBehavior.Default)
        {
            //todo: params
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbCommand cmd, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default)
        {
            //todo: params
            return cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public static DbDataReader ExecuteReader(this DbConnection connection, string sql, object args = null, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteReader(behavior);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandBehavior behavior = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteReaderAsync(behavior, cancellationToken);
        }
    }
}
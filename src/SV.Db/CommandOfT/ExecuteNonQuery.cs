using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuery(this DbCommand cmd, object args = null)
        {
            cmd.SetParams(args);
            return cmd.ExecuteNonQuery();
        }

        public static Task<int> ExecuteNonQueryAsync(this DbCommand cmd, object args = null, CancellationToken cancellationToken = default)
        {
            cmd.SetParams(args);
            return cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public static int ExecuteNonQuery(this DbConnection connection, string sql, object args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteNonQuery();
        }

        public static Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            cmd.SetParams(args);
            return cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
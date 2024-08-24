using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static int ExecuteNonQuery(this DbConnection connection, string sql, object args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteNonQuery();
        }

        public static Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql, object args = null, CancellationToken cancellationToken = default, CommandType commandType = CommandType.Text)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = commandType;
            //todo: params
            return cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
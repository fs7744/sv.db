using System.Data;
using System.Data.Common;

namespace SV.Db
{
    public static partial class CommandExtensions
    {
        public static IEnumerable<T> ExecuteScalars<T>(this DbCommand command, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0, bool useBuffer = true)
        {
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
                using (var reader = command.ExecuteReader(behavior))
                {
                    var r = reader.ReadEnumerable<T>(estimateRow, useBuffer);
                    while (reader.NextResult()) { }
                    return r;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public static IEnumerable<T> ExecuteScalars<T>(this DbCommand command, object args, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0, bool useBuffer = true)
        {
            return command.ExecuteScalars<T>(behavior, estimateRow, useBuffer);
        }

        public static async Task<IEnumerable<T>> ExecuteScalarsAsync<T>(this DbCommand command, object args, CancellationToken cancellationToken, CommandBehavior behavior = CommandBehavior.Default, int estimateRow = 0, bool useBuffer = true)
        {
            var connection = command.Connection;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                {
                    var r = reader.ReadEnumerable<T>(estimateRow, useBuffer);
                    while (await reader.NextResultAsync()) { }
                    return r;
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
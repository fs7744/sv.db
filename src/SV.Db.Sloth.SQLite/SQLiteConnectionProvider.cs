using Microsoft.Data.Sqlite;
using SV.Db.Sloth.Statements;
using System.Collections.Frozen;
using System.Data.Common;
using System.Text;

namespace SV.Db.Sloth.SQLite
{
    public partial class SQLiteConnectionProvider : IDbConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return TransactionConnectionFactory.GetOrAdd(connectionString, s => new SqliteConnection(s));
        }

        private static void ConvertJsonField(Statement v, StringBuilder sb, bool allowAs, FrozenDictionary<string, string> fs, JsonFieldStatement js)
        {
            sb.Append("json_extract(");
            sb.Append(fs[js.Field]);
            sb.Append(",");
            sb.Append("'");
            sb.Append(js.Path.Replace("'", "\\'"));
            sb.Append("'");
            sb.Append(")");
            if (allowAs && !string.IsNullOrWhiteSpace(js.As))
            {
                sb.Append(" as ");
                sb.Append(js.As);
            }
            if (v is IOrderByField orderBy)
            {
                sb.Append(" ");
                sb.Append(Enums<OrderByDirection>.GetName(orderBy.Direction));
            }
        }
    }
}
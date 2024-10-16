﻿using Npgsql;
using SV.Db.Sloth.Statements;
using System.Collections.Frozen;
using System.Data.Common;
using System.Text;

namespace SV.Db.Sloth.PostgreSQL
{
    public partial class PostgreSQLConnectionProvider : IConnectionProvider
    {
        public DbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        private static void ConvertJsonField(Statement v, StringBuilder sb, bool allowAs, FrozenDictionary<string, string> fs, JsonFieldStatement js)
        {
            sb.Append("jsonb_path_query_first(");
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
using Lmzzz.Template;
using System.Data;
using System.Data.Common;

namespace SV.Db.Sloth;

public class DbCodeGenerater
{
    string Template = """"
        [Table("""
            select {Fields}
            FROM {{Name}} a
            {where}
            """, UpdateTable = "{{Name}}")]
        public class {{Name}}
        {
            {{for ( c,i in Columns)}}[Select("a.{{c.ColumnName}}"), OrderBy, Where, Column(Name = "{{c.ColumnName}}", Type = DbType.{{c.DbTypeName}}), Update{{if(c.IsPrimaryKey == true)}}(PrimaryKey = true, NotAllowInsert = true){{endif}}]
            public {{c.TypeName}}? {{c.ColumnName}} { get; set; }
            {{endfor}}
        }
        """";

    public string GenerateMysqlCode(DbConnection connection, string table)
    {
        try
        {
            connection.Open();
            var t = connection.GetSchema("Columns", new string[] { null, null, table });
            var td = new TableData()
            {
                Name = table,
                Columns = new List<ColumnData>()
            };

            for (var i = 0; i < t.Rows.Count; i++)
            {
                var r = t.Rows[i];
                var c = new ColumnData()
                {
                    ColumnName = r["COLUMN_NAME"].ToString(),
                    IsNullable = r["IS_NULLABLE"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                    DataType = r["DATA_TYPE"].ToString().ToLower(),
                    IsPrimaryKey = r["COLUMN_KEY"].ToString().Equals("PRI", StringComparison.OrdinalIgnoreCase),
                };
                c.Init();
                td.Columns.Add(c);
            }
            return Template.EvaluateTemplate(td);
        }
        finally
        {
            connection.Close();
        }

    }

    public class TableData
    {
        public string Name { get; set; }

        public List<ColumnData> Columns { get; set; }
    }

    public class ColumnData
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public DbType DbType { get; set; }

        public string DbTypeName { get; set; }

        public string TypeName { get; set; }

        internal void Init()
        {
            DbType = DataType switch
            {
                "int" => DbType.Int32,
                "tinyint" => DbType.Int32,
                "bigint" => DbType.Int64,
                "bit" => DbType.Boolean,
                "datetime" => DbType.DateTime,
                "decimal" => DbType.Decimal,
                _ => DbType.String,
            };
            DbTypeName = Enum.GetName<DbType>(DbType);

            TypeName = DbType switch
            {
                DbType.Int32 => "int",
                DbType.Int64 => "long",
                DbType.Boolean => "bool",
                DbType.DateTime => "DateTime",
                DbType.Decimal => "decimal",
                _ => "string",
            };
        }
    }
}

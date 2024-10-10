using System.Data;

namespace SV.Db
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string? Name { get; set; }

        public DbType Type { get; set; }

        public ParameterDirection Direction { get; set; }

        public byte Precision { get; set; }

        public byte Scale { get; set; }

        public int Size { get; set; }

        public string? CustomConvertToDbMethod { get; set; }

        public string? CustomConvertFromDbMethod { get; set; }

        public bool IsJson { get; set; }
    }
}
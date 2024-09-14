using System.Data;

namespace SV.Db
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string? Name { get; set; }

        public DbType Type { get; set; }

        public ParameterDirection Direction { get; set; }

        public byte Precision { get; set; }

        public byte Scale { get; set; }

        public byte Size { get; set; }

        public string? CustomConvertMethod { get; set; }
    }
}
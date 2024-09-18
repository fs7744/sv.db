namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string table)
        {
            Table = table;
        }

        public string Table { get; }
    }
}
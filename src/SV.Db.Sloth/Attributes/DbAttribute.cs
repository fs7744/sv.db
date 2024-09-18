namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public class DbAttribute : Attribute
    {
        public DbAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
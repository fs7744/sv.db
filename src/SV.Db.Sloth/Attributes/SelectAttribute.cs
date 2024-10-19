namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class SelectAttribute : Attribute
    {
        public SelectAttribute(string field)
        {
            Field = field;
        }

        public string Field { get; }

        public bool NotAllow { get; set; }
    }
}
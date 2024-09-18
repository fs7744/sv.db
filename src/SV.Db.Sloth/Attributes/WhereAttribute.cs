namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class WhereAttribute : Attribute
    {
        public string Field { get; set; }

        public bool NotAllow { get; set; }
    }
}
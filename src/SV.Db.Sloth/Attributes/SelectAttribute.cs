namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class SelectAttribute : Attribute
    {
        public string Field { get; set; }

        public bool NotAllow { get; set; }
    }
}
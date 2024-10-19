namespace SV.Db.Sloth.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UpdateAttribute : Attribute
    {
        public string Field { get; set; }
        public bool NotAllowInsert { get; set; }
        public bool PrimaryKey { get; set; }
    }
}
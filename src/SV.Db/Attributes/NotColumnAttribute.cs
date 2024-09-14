namespace SV.Db
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotColumnAttribute : Attribute
    {
    }
}
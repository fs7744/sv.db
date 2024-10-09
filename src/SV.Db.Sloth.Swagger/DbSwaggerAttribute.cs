namespace SV.Db.Sloth.Swagger
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DbSwaggerAttribute : Attribute
    {
        public DbSwaggerAttribute(string key)
        {
            this.Key = key;
        }

        public string Key { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DbSwaggerByTypeAttribute : Attribute
    {
        public DbSwaggerByTypeAttribute(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; }
    }
}
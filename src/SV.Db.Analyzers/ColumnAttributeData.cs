namespace SV.Db.Analyzers
{
    public sealed class ColumnAttributeData
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Direction { get; set; }
        public string? Precision { get; set; }
        public string? Scale { get; set; }
        public string? Size { get; set; }
        public string? CustomConvertToDbMethod { get; set; }
        public string? CustomConvertFromDbMethod { get; set; }

        public string GetName(string defaultValue)
        {
            return string.IsNullOrWhiteSpace(Name) ? defaultValue : Name.Substring(1, Name.Length - 2);
        }

        public string GetCustomConvertToDbMethod()
        {
            return string.IsNullOrWhiteSpace(CustomConvertToDbMethod) ? null : CustomConvertToDbMethod.Substring(1, CustomConvertToDbMethod.Length - 2);
        }

        public string GetCustomConvertFromDbMethod()
        {
            return string.IsNullOrWhiteSpace(CustomConvertFromDbMethod) ? null : CustomConvertFromDbMethod.Substring(1, CustomConvertFromDbMethod.Length - 2);
        }

        public override string ToString()
        {
            return $"ColumnAttributeData: Name:{Name},Type:{Type},Direction:{Direction},Precision:{Precision},Scale:{Scale},Size:{Size},CustomConvertToDbMethod:{CustomConvertToDbMethod},CustomConvertFromDbMethod:{CustomConvertFromDbMethod}";
        }
    }
}
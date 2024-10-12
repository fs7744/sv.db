namespace SV.Db.Sloth.Statements
{
    public class JsonFieldStatement : FieldStatement
    {
        public string Path { get; set; }
        public string As { get; set; }
    }

    public class JsonOrderByFieldStatement : JsonFieldStatement, IOrderByField
    {
        public OrderByDirection Direction { get; set; }
    }
}
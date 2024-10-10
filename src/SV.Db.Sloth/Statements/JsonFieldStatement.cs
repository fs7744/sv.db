namespace SV.Db.Sloth.Statements
{
    public class JsonFieldStatement : FieldStatement
    {
        public string Path { get; set; }
        public string As { get; set; }
    }
}
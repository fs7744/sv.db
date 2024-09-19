namespace SV.Db.Sloth.Statements
{
    public class LimitStatement : Statement
    {
        public int Rows { get; set; }
        public int? Offset { get; set; }
    }
}
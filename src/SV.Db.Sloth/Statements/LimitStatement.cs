namespace SV.Db.Sloth.Statements
{
    public class LimitStatement : Statement
    {
        public int Rows { get; private set; }
        public int? Offset { get; private set; }
    }
}
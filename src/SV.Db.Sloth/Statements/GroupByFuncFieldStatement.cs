namespace SV.Db.Sloth.Statements
{
    public class GroupByFuncFieldStatement : FieldStatement
    {
        public string Func { get; set; }
        public string As { get; set; }
    }
}
namespace SV.Db.Sloth.Statements
{
    public class ValueConditionStatement : ConditionStatement
    {
        public ValueStatement Value { get; set; }
        public OperaterStatement Operater { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Operater?.Visit(visitor);
        }
    }
}
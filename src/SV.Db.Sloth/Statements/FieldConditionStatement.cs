namespace SV.Db.Sloth.Statements
{
    public class FieldConditionStatement : ConditionStatement
    {
        public string Field { get; set; }
        public OperaterStatement Operater { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Operater?.Visit(visitor);
        }
    }
}
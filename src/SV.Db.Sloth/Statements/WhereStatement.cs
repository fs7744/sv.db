namespace SV.Db.Sloth.Statements
{
    public class WhereStatement : ConditionStatement
    {
        public ConditionStatement Condition { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Condition?.Visit(visitor);
        }
    }
}
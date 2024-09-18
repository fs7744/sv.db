namespace SV.Db.Sloth.Statements
{
    public abstract class OperaterValueStatement : Statement
    {
        public abstract string Operater { get; }

        public ValueStatement Value { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Value?.Visit(visitor);
        }
    }
}
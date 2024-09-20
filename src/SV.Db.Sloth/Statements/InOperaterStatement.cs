namespace SV.Db.Sloth.Statements
{
    public class InOperaterStatement : ConditionStatement
    {
        public ValueStatement Left { get; set; }
        public string Operater => "in";
        public ArrayValueStatement Right { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Left?.Visit(visitor);
            Right?.Visit(visitor);
        }
    }
}
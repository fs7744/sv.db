namespace SV.Db.Sloth.Statements
{
    public class LikeOperaterStatement : OperaterStatement
    {
        public override string Operater => "like";
        public StringValueStatement Value { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Value?.Visit(visitor);
        }
    }
}
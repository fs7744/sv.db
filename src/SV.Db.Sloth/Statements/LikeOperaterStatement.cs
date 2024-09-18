namespace SV.Db.Sloth.Statements
{
    public class LikeOperaterStatement : OperaterStatement
    {
        public ValueStatement Left { get; set; }
        public string Operater => "like";
        public StringValueStatement Right { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Left?.Visit(visitor);
            Right?.Visit(visitor);
        }
    }
}
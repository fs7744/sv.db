namespace SV.Db.Sloth.Statements
{
    public class InOperaterStatement : ConditionStatement
    {
        public ValueStatement Left { get; set; }
        public string Operater => "in";
        public List<ValueStatement> Right { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Left?.Visit(visitor);
            if (Right != null)
            {
                foreach (var item in Right)
                {
                    visitor(item);
                }
            }
        }
    }
}
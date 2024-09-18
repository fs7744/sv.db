namespace SV.Db.Sloth.Statements
{
    public class InOperaterStatement : OperaterStatement
    {
        public override string Operater => "in";
        public List<ValueStatement> Value { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            if (Value != null)
            {
                foreach (var item in Value)
                {
                    visitor(item);
                }
            }
        }
    }
}
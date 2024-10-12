namespace SV.Db.Sloth.Statements
{
    public class OrderByStatement : Statement
    {
        public List<FieldStatement> Fields { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            if (Fields != null)
            {
                foreach (var item in Fields)
                {
                    visitor(item);
                }
            }
        }
    }
}
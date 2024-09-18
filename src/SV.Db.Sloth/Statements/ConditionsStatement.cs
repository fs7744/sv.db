namespace SV.Db.Sloth.Statements
{
    public class ConditionsStatement : Statement
    {
        public List<ConditionStatement> Children { get; set; }
        public Condition Condition { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            if (Children != null)
            {
                foreach (var item in Children)
                {
                    visitor(item);
                }
            }
        }
    }
}
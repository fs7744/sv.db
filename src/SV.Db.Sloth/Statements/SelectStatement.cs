namespace SV.Db.Sloth.Statements
{
    public class SelectStatement : Statement
    {
        public SelectFieldsStatement Fields { get; set; }
        public FromStatement From { get; set; }

        public WhereStatement Where { get; set; }

        public OrderByStatement OrderBy { get; set; }
        public LimitStatement Limit { get; set; }

        public bool HasTotalCount { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Fields?.Visit(visitor);
            From?.Visit(visitor);
            Where?.Visit(visitor);
            OrderBy?.Visit(visitor);
            Limit?.Visit(visitor);
        }
    }
}
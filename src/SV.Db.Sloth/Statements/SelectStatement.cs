﻿namespace SV.Db.Sloth.Statements
{
    public class SelectStatement : Statement
    {
        public List<FieldStatement>? Fields { get; set; }

        public WhereStatement Where { get; set; }

        public List<FieldStatement>? OrderBy { get; set; }

        public int Rows { get; set; } = 10;
        public int? Offset { get; set; }

        public bool HasTotalCount { get; set; }

        public List<FieldStatement>? GroupBy { get; set; }

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
            Where?.Visit(visitor);
            if (OrderBy != null)
            {
                foreach (var item in OrderBy)
                {
                    visitor(item);
                }
            }
            if (GroupBy != null)
            {
                foreach (var item in GroupBy)
                {
                    visitor(item);
                }
            }
        }
    }
}
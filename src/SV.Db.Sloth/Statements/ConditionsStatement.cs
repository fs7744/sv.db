﻿namespace SV.Db.Sloth.Statements
{
    public class ConditionsStatement : ConditionStatement
    {
        public Condition Condition { get; set; }
        public ConditionStatement Left { get; set; }
        public ConditionStatement Right { get; set; }

        public override void Visit(Action<Statement> visitor)
        {
            visitor(this);
            Left?.Visit(visitor);
            Right?.Visit(visitor);
        }
    }
}
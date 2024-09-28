using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public class SelectStatementBuilder<T> : SelectStatementBuilder
    {
    }

    public class SelectStatementBuilder
    {
        internal readonly SelectStatement statement = new SelectStatement() { Limit = new LimitStatement() { Rows = 10 } };
        internal DbEntityInfo dbEntityInfo;
        internal IConnectionFactory factory;

        public SelectStatement Build() => statement;
    }
}
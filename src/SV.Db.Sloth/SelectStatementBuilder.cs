using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public class SelectStatementBuilder<T>
    {
        internal readonly SelectStatement statement = new SelectStatement();

        public SelectStatement Build() => statement;
    }
}
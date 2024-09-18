using System.Linq.Expressions;

namespace SV.Db.Sloth
{
    public static class Query
    {
        public static SelectStatementBuilder<T> From<T>()
        {
            return new SelectStatementBuilder<T>();
        }

        public static SelectStatementBuilder<T> Select<T>(this SelectStatementBuilder<T> select, params string[] fields)
        {
            return select;
        }

        public static SelectStatementBuilder<T> Select<T>(this SelectStatementBuilder<T> select, params Expression<Func<T, object>>[] exprs)
        {
            return select.Select<T>(exprs.Select(i => i.GetMemberName()).ToArray());
        }

        public static SelectStatementBuilder<T> Where<T>(this SelectStatementBuilder<T> select, Expression<Func<T, bool>> expr)
        {
            var a = expr.Body as BinaryExpression;
            var b = a.Right as MethodCallExpression;
            return select;
        }
    }
}
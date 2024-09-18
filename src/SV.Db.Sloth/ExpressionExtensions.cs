using System.Linq.Expressions;

namespace SV.Db
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName<T>(this Expression<T> expression) => expression.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
            _ => throw new NotImplementedException(expression.GetType().ToString())
        };

        public static bool Like<T>(this T o, string s)
        {
            throw new NotImplementedException();
        }

        public static bool PrefixLike<T>(this T o, string s)
        {
            throw new NotImplementedException();
        }

        public static bool SuffixLike<T>(this T o, string s)
        {
            throw new NotImplementedException();
        }
    }
}
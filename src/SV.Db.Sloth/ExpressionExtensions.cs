using System.IO;
using System.Linq.Expressions;

namespace SV.Db
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName<T>(this Expression<T> expression) => expression.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
            MethodCallExpression methodCallExpression => GetMethodCallMemberName(methodCallExpression),
            _ => throw new NotImplementedException(expression.GetType().ToString())
        };

        public static string GetMemberName(Expression expression) => expression switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m => m.Member.Name,
            MethodCallExpression methodCallExpression => GetMethodCallMemberName(methodCallExpression),
            _ => throw new NotImplementedException(expression.GetType().ToString())
        };

        public static string GetMethodCallMemberName(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case nameof(JsonExtract):
                    return GetMemberName(expression);

                case nameof(Desc):
                    return $"{GetMemberName(expression.Arguments[0])} desc";

                default:
                    throw new NotImplementedException(expression.GetType().ToString());
            }
        }

        public static string GetMemberName(MethodCallExpression expression)
        {
            var path = Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke().ToString().Replace("'", "\\'");
            var aS = expression.Arguments.Count > 2 ? "," + Expression.Lambda(expression.Arguments[2]).Compile().DynamicInvoke().ToString() : string.Empty;
            return $"json({GetMemberName(expression.Arguments[0])},'{path}'{aS})";
        }

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

        public static bool In<R>(this object o, params R[] source)
        {
            throw new NotImplementedException();
        }

        public static object JsonExtract(this object o, string path)
        {
            throw new NotImplementedException();
        }

        public static object JsonExtract(this object o, string path, string aS)
        {
            throw new NotImplementedException();
        }

        public static object Desc(this object o)
        {
            throw new NotImplementedException();
        }
    }
}
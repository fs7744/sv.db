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
                    return GetJsonMemberName(expression);

                case nameof(Min):
                case nameof(Max):
                case nameof(Sum):
                case nameof(Count):
                    return GetFuncMemberName(expression);

                case nameof(Desc):
                    return $"{GetMemberName(expression.Arguments[0])} desc";

                default:
                    throw new NotImplementedException(expression.GetType().ToString());
            }
        }

        public static string GetJsonMemberName(MethodCallExpression expression)
        {
            var path = Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke().ToString().Replace("'", "\\'");
            var aS = expression.Arguments.Count > 2 ? "," + Expression.Lambda(expression.Arguments[2]).Compile().DynamicInvoke().ToString() : string.Empty;
            return $"json({GetMemberName(expression.Arguments[0])},'{path}'{aS})";
        }

        public static string GetFuncMemberName(MethodCallExpression expression)
        {
            var aS = expression.Arguments.Count > 1 ? "," + Expression.Lambda(expression.Arguments[1]).Compile().DynamicInvoke().ToString() : string.Empty;
            return $"{expression.Method.Name}({GetMemberName(expression.Arguments[0])}{aS})";
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

        public static Any JsonExtract(this object o, string path)
        {
            throw new NotImplementedException();
        }

        public static Any JsonExtract(this object o, string path, string aS)
        {
            throw new NotImplementedException();
        }

        public static object Desc(this object o)
        {
            throw new NotImplementedException();
        }

        public static Any Count(this object o, string aS)
        {
            throw new NotImplementedException();
        }

        public static Any Min(this object o, string aS)
        {
            throw new NotImplementedException();
        }

        public static Any Max(this object o, string aS)
        {
            throw new NotImplementedException();
        }

        public static Any Sum(this object o, string aS)
        {
            throw new NotImplementedException();
        }
    }

    public class Any
    {
        public static bool operator ==(Any obj1, Any obj2) => throw new NotImplementedException();

        public static bool operator !=(Any obj1, Any obj2) => throw new NotImplementedException();

        public static bool operator >=(Any obj1, Any obj2) => throw new NotImplementedException();

        public static bool operator <=(Any obj1, Any obj2) => throw new NotImplementedException();

        public static bool operator >(Any obj1, Any obj2) => throw new NotImplementedException();

        public static bool operator <(Any obj1, Any obj2) => throw new NotImplementedException();

        public static implicit operator int(Any d) => throw new NotImplementedException();

        public static implicit operator decimal(Any d) => throw new NotImplementedException();

        public static implicit operator double(Any d) => throw new NotImplementedException();

        public static implicit operator string(Any d) => throw new NotImplementedException();

        public static implicit operator float(Any d) => throw new NotImplementedException();

        public static implicit operator long(Any d) => throw new NotImplementedException();

        public static implicit operator bool(Any d) => throw new NotImplementedException();

        //public static explicit operator Any(byte b) => new Digit(b);
    }
}
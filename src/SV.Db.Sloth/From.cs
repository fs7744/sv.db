using SV.Db.Sloth.Statements;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace SV.Db.Sloth
{
    public static class From
    {
        public static SelectStatementBuilder<T> Of<T>()
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
            ConditionStatement r = ConvertConditionStatement(expr?.Body);
            if (r == null) throw new NotSupportedException(expr.ToString());
            select.statement.Where = new WhereStatement() { Condition = r };
            return select;
        }

        private static ConditionStatement ConvertConditionStatement(Expression expr)
        {
            if (expr == null) return null;
            ConditionStatement r;
            if (expr is MemberExpression v)
            {
                r = new OperaterStatement() { Operater = "=", Left = ConvertValueStatement(v), Right = new BooleanValueStatement() { Value = true } };
            }
            else if (expr.NodeType == ExpressionType.Not && expr is UnaryExpression ue)
            {
                r = new UnaryOperaterStatement() { Operater = "not", Right = ConvertConditionStatement(ue.Operand) };
            }
            else if (expr is BinaryExpression bExpr)
            {
                r = ConvertOperaterStatement(bExpr);
                if (r == null)
                {
                    switch (expr.NodeType)
                    {
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                            r = new ConditionsStatement() { Condition = Condition.And, Left = ConvertConditionStatement(bExpr.Left), Right = ConvertConditionStatement(bExpr.Right) };
                            break;

                        case ExpressionType.Or:
                        case ExpressionType.OrElse:
                            r = new ConditionsStatement() { Condition = Condition.Or, Left = ConvertConditionStatement(bExpr.Left), Right = ConvertConditionStatement(bExpr.Right) };
                            break;

                        default:
                            return null;
                    }
                }
            }
            else
            {
                r = null;
            }
            return r;
        }

        private static ConditionStatement ConvertOperaterStatement(BinaryExpression bExpr)
        {
            switch (bExpr.NodeType)
            {
                case ExpressionType.Equal:
                    return new OperaterStatement() { Operater = "=", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                case ExpressionType.GreaterThan:
                    return new OperaterStatement() { Operater = ">", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                case ExpressionType.GreaterThanOrEqual:
                    return new OperaterStatement() { Operater = ">=", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                case ExpressionType.LessThan:
                    return new OperaterStatement() { Operater = "<", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                case ExpressionType.LessThanOrEqual:
                    return new OperaterStatement() { Operater = "<=", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                case ExpressionType.NotEqual:
                    return new OperaterStatement() { Operater = "!=", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };

                default:
                    return null;
            }
        }

        private static ValueStatement ConvertValueStatement(Expression v)
        {
            if (v != null)
            {
                if (v.NodeType == ExpressionType.Convert) return ConvertValueStatement((v as UnaryExpression).Operand);
                if (v is MemberExpression m && m.Expression != null)
                {
                    if (m.Expression.NodeType == ExpressionType.Parameter)
                    {
                        return new FieldValueStatement() { Field = m.Member.Name };
                    }
                    else
                    {
                        var o = Expression.Lambda(m).Compile().DynamicInvoke();
                        return ConvertConstantStatement(o);
                    }
                }
                else if (v is ConstantExpression constant)
                {
                    return ConvertConstantStatement(constant.Value);
                }
                else if (v.NodeType == ExpressionType.Call)
                {
                    var o = Expression.Lambda(v).Compile().DynamicInvoke();
                    return ConvertConstantStatement(o);
                }
            }
            throw new NotSupportedException(v.ToString());
        }

        private static ValueStatement ConvertConstantStatement(object v)
        {
            if (v == null)
            {
                return new NullValueStatement();
            }
            else if (v is string s)
            {
                return new StringValueStatement() { Value = s };
            }
            else if (v is bool b)
            {
                return new BooleanValueStatement() { Value = b };
            }
            else
            {
                return new NumberValueStatement() { Value = Convert.ToDecimal(v) };
            }
        }
    }
}
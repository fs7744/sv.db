using SV.Db.Sloth.Statements;
using System.Collections;
using System.Linq.Expressions;

namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static SelectStatementBuilder<T> Of<T>()
        {
            var r = new SelectStatementBuilder<T>();
            r.statement.Fields = new SelectFieldsStatement { Fields = new List<FieldStatement> { new FieldStatement { Name = "*" } } };
            return r;
        }

        public static SelectStatementBuilder<T> Select<T>(this SelectStatementBuilder<T> select, params string[] fields)
        {
            var f = select.statement.Fields.Fields;
            foreach (var item in fields.Where(j => !f.Any(i => i.Name.Equals(j, StringComparison.OrdinalIgnoreCase))))
            {
                f.Add(new FieldStatement() { Name = item });
            }
            var fs = f.Count(i => i is FieldStatement) > 1 ? f.FirstOrDefault(i => i.Name.Equals("*", StringComparison.OrdinalIgnoreCase)) : null;
            if (fs != null)
            {
                f.Remove(fs);
            }

            return select;
        }

        public static SelectStatementBuilder<T> WithTotalCount<T>(this SelectStatementBuilder<T> select)
        {
            if (!select.statement.Fields.Fields.Any(i => i.Name.Equals("count()", StringComparison.OrdinalIgnoreCase)))
            {
                select.statement.Fields.Fields.Add(new FuncCallerStatement() { Name = "count()" });
            }

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

        public static SelectStatementBuilder<T> Limit<T>(this SelectStatementBuilder<T> select, int rows, int? offset = null)
        {
            var l = select.statement.Limit;
            l.Offset = offset;
            l.Rows = rows;
            return select;
        }

        public static SelectStatementBuilder<T> OrderBy<T>(this SelectStatementBuilder<T> select, params (string, OrderByDirection)[] fields)
        {
            select.statement.OrderBy = new OrderByStatement() { Fields = fields.Select(i => new OrderByFieldStatement() { Name = i.Item1, Direction = i.Item2 }).ToList() };
            return select;
        }

        public static SelectStatementBuilder<T> OrderBy<T>(this SelectStatementBuilder<T> select, params (Expression<Func<T, object>>, OrderByDirection)[] fields)
        {
            return select.OrderBy<T>(fields.Select(i => (i.Item1.GetMemberName(), i.Item2)).ToArray());
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
            else if (expr.NodeType == ExpressionType.Call)
            {
                r = ConvertFuncOperaterStatement(expr);
            }
            else
            {
                r = null;
            }
            return r;
        }

        private static ConditionStatement? ConvertFuncOperaterStatement(Expression expr)
        {
            if (expr is MethodCallExpression m)
            {
                if (m.Arguments.Count != 2)
                {
                    return null;
                }
                FieldValueStatement f = null;
                if (m.Arguments[0] is MemberExpression me && me.Expression != null)
                {
                    if (me.Expression.NodeType == ExpressionType.Parameter)
                    {
                        f = new FieldValueStatement() { Field = me.Member.Name };
                    }
                }

                if (f == null)
                    return null;

                var o = Expression.Lambda(m.Arguments[1]).Compile().DynamicInvoke();

                if (o == null)
                    return null;

                switch (m.Method.Name)
                {
                    case "Like":
                        return new OperaterStatement() { Operater = "like", Left = f, Right = new StringValueStatement { Value = o.ToString() } };

                    case "PrefixLike":
                        return new OperaterStatement() { Operater = "prefix-like", Left = f, Right = new StringValueStatement { Value = o.ToString() } };

                    case "SuffixLike":
                        return new OperaterStatement() { Operater = "suffix-like", Left = f, Right = new StringValueStatement { Value = o.ToString() } };

                    case "In":
                        if (o is IEnumerable<string> s)
                            return new InOperaterStatement() { Left = f, Right = new StringArrayValueStatement() { Value = s.AsList() } };
                        else if (o is IEnumerable<bool> b)
                            return new InOperaterStatement() { Left = f, Right = new BooleanArrayValueStatement() { Value = b.AsList() } };
                        else if (o is IEnumerable<decimal> d)
                            return new InOperaterStatement() { Left = f, Right = new NumberArrayValueStatement() { Value = d.AsList() } };
                        else if (o is IEnumerable dd)
                        {
                            var list = new List<decimal>();
                            foreach (var item in dd)
                            {
                                list.Add(Convert.ToDecimal(item));
                            }
                            return new InOperaterStatement() { Left = f, Right = new NumberArrayValueStatement() { Value = list } };
                        }
                        else
                            return null;

                    default:
                        return null;
                }
            }
            return null;
        }

        private static ConditionStatement ConvertOperaterStatement(BinaryExpression bExpr)
        {
            switch (bExpr.NodeType)
            {
                case ExpressionType.Equal:
                    var r = new OperaterStatement() { Operater = "=", Left = ConvertValueStatement(bExpr.Left), Right = ConvertValueStatement(bExpr.Right) };
                    if (r.Right is NullValueStatement)
                    {
                        r.Operater = "is-null";
                    }
                    return r;

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
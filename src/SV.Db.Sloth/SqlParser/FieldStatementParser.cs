using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public class FieldStatementParser : IStatementParser
    {
        public bool TryParse(StatementParserContext context)
        {
            if (context.HasToken())
            {
                var c = context.Current;
                if (context.State == StatementState.Fields)
                {
                    if (c.Type == TokenType.Sign && c.GetValue().Equals(",", StringComparison.Ordinal))
                    {
                        if (context.MoveNext())
                        {
                            c = context.Current;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                if (c.Type == TokenType.Word)
                {
                    var v = c.GetValue();
                    if (OperaterStatementParser.TryConvertJsonField(v, context) || TryGroupByFuncField(v, context))
                    {
                        context.State = StatementState.Fields;
                        return true;
                    }
                    else
                    {
                        if ((context.ParseType & ParseType.OrderByField) == ParseType.OrderByField)
                        {
                            var f = new OrderByFieldStatement() { Field = v.ToString() };
                            context.Stack.Push(f);
                            if (context.MoveNext())
                            {
                                var t = context.Current;
                                if (t.Type == TokenType.Word)
                                {
                                    var vv = t.GetValue();
                                    if (vv.Equals("asc", StringComparison.OrdinalIgnoreCase) || vv.Equals("desc", StringComparison.OrdinalIgnoreCase))
                                    {
                                        f.Direction = Enums<OrderByDirection>.Parse(vv.ToString(), true);
                                        context.MoveNext();
                                    }
                                }
                            }
                            else
                            {
                                f.Direction = OrderByDirection.Asc;
                            }
                        }
                        else
                        {
                            context.Stack.Push(new FieldStatement() { Field = v.ToString() });
                            context.MoveNext();
                        }
                        context.State = StatementState.Fields;
                        return true;
                    }
                }
                else if ((context.ParseType & ParseType.SelectField) == ParseType.SelectField && c.Type == TokenType.Sign && c.GetValue().Equals("*", StringComparison.Ordinal))
                {
                    context.Stack.Push(new FieldStatement() { Field = "*" });
                    context.MoveNext();
                    context.State = StatementState.Fields;
                    return true;
                }
            }
            return false;
        }

        private static void SkipComma(StatementParserContext context)
        {
            if (context.HasToken())
            {
                var c = context.Current;
                if (c.Type == TokenType.Sign && c.GetValue().Equals(",", StringComparison.Ordinal))
                {
                    context.MoveNext();
                }
            }
        }

        internal static bool TryGroupByFuncField(ReadOnlySpan<char> v, StatementParserContext context)
        {
            if (v.Equals("count", StringComparison.OrdinalIgnoreCase)
                || v.Equals("min", StringComparison.OrdinalIgnoreCase)
                || v.Equals("max", StringComparison.OrdinalIgnoreCase)
                || v.Equals("sum", StringComparison.OrdinalIgnoreCase))
            {
                var index = context.Index;
                if (context.MoveNext())
                {
                    var t = context.Current;
                    if (t.Type == TokenType.Sign
                        && t.GetValue().Equals("(", StringComparison.Ordinal))
                    {
                        if ((context.ParseType & ParseType.GrGroupByFuncField) == ParseType.GrGroupByFuncField)
                        {
                            var op = new GroupByFuncFieldStatement() { Func = v.ToString() };
                            context.Stack.Push(op);
                            if (ConvertGroupByFuncFieldStatement(context, op) && context.Stack.Peek() == op)
                            {
                                return true;
                            }
                        }
                        throw new ParserExecption($"Can't parse near by {t.GetValue()} (Line:{t.StartLine},Col:{t.StartColumn})");
                    }
                }
                context.Index = index;
            }
            return false;
        }

        private static bool ConvertGroupByFuncFieldStatement(StatementParserContext context, GroupByFuncFieldStatement op)
        {
            Token t;
            if (context.MoveNext())
            {
                t = context.Current;
                if (t.Type != TokenType.Word)
                {
                    return false;
                }
                op.Field = t.GetValue().ToString();
                if (!context.MoveNext())
                {
                    return false;
                }
                t = context.Current;
                if (t.Type == TokenType.Sign && t.GetValue().Equals(",", StringComparison.Ordinal))
                {
                    if (!context.MoveNext())
                    {
                        return false;
                    }
                    t = context.Current;
                    if (t.Type != TokenType.Word)
                    {
                        return false;
                    }
                    op.As = t.GetValue().ToString();
                    if (!context.MoveNext())
                    {
                        return false;
                    }
                }
                t = context.Current;
                if (t.Type != TokenType.Sign && !t.GetValue().Equals(")", StringComparison.Ordinal))
                {
                    return false;
                }
                context.MoveNext();

                return true;
            }
            return false;
        }
    }
}
using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public class OperaterStatementParser : IStatementParser
    {
        public bool TryParse(StatementParserContext context)
        {
            if (context.HasToken())
            {
                var c = context.Current;
                if (c.Type == TokenType.Sign)
                {
                    return ConvertSign(context, ref c);
                }
                else if (c.Type == TokenType.Word)
                {
                    var v = c.GetValue();
                    if (v.Equals("like", StringComparison.OrdinalIgnoreCase))
                    {
                        return ConvertLike(context, ref c, v);
                    }
                    else if (v.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        var op = new InOperaterStatement();
                        if (context.MoveNext() && context.Stack.Peek() is FieldValueStatement vs)
                        {
                            var index = context.Index;
                            op.Left = vs;
                            context.Stack.Pop();
                            context.Stack.Push(op);
                            context.Parse(context);
                            if (context.Stack.TryPop(out var vsss) && vsss is ArrayValueStatement vss)
                            {
                                if (context.Stack.Peek() == op)
                                {
                                    return true;
                                }
                            }
                            context.Index = index;
                            c = context.Current;
                        }
                        throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                    }
                    else if (v.Equals("not", StringComparison.OrdinalIgnoreCase))
                    {
                        var op = new UnaryOperaterStatement() { Operater = "not" };
                    }
                }
            }
            return false;
        }

        private static bool ConvertSign(StatementParserContext context, ref Token c)
        {
            var v = c.GetValue();
            var s = v.ToString();
            var index = context.Index;
            switch (s)
            {
                case "<":
                case "<=":
                case ">":
                case ">=":
                case "=":
                case "!=":
                    {
                        var op = new OperaterStatement();
                        op.Operater = s;
                        if (context.MoveNext() && context.Stack.Peek() is ValueStatement vs)
                        {
                            op.Left = vs;
                            context.Stack.Pop();
                            context.Stack.Push(op);
                            context.Parse(context);
                            if (context.Stack.TryPop(out var vsss) && vsss is ValueStatement vss)
                            {
                                if (s == "=" && vss is FieldValueStatement f && f.Field.Equals("null", StringComparison.OrdinalIgnoreCase))
                                {
                                    op.Operater = "is-null";
                                }
                                else
                                {
                                    op.Right = vss;
                                }

                                if (context.Stack.Peek() == op)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    break;

                case "(":
                    if (context.MoveNext())
                    {
                        var t = context.Current;
                        if (t.Type == TokenType.Number)
                        {
                            if (ConvertNumberArrary(context, t, out var op))
                            {
                                context.Stack.Push(op);
                                return true;
                            }
                        }
                        else if (t.Type == TokenType.String)
                        {
                            if (ConvertStringArrary(context, t, out var op))
                            {
                                context.Stack.Push(op);
                                return true;
                            }
                        }
                    }
                    break;
            }
            context.Index = index;
            c = context.Current;
            throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
        }

        private static bool ConvertStringArrary(StatementParserContext context, Token t, out Statement o)
        {
            var op = new StringArrayValueStatement() { Value = new List<string>() { t.GetValue().ToString() } };
            o = op;
            var hasEnd = false;
            while (context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Sign)
                {
                    var tv = t.GetValue().ToString();
                    if (tv.Equals(","))
                    {
                        if (context.MoveNext())
                        {
                            t = context.Current;
                            if (t.Type == TokenType.String)
                            {
                                op.Value.Add(t.GetValue().ToString());
                                continue;
                            }
                        }
                    }
                    else if (tv.Equals(")"))
                    {
                        context.MoveNext();
                        hasEnd = true;
                        break;
                    }
                }

                break;
            }

            return hasEnd;
        }

        private static bool ConvertNumberArrary(StatementParserContext context, Token t, out Statement o)
        {
            var op = new NumberArrayValueStatement() { Value = new List<decimal>() { decimal.Parse(t.GetValue()) } };
            o = op;
            var hasEnd = false;
            while (context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Sign)
                {
                    var tv = t.GetValue().ToString();
                    if (tv.Equals(","))
                    {
                        if (context.MoveNext())
                        {
                            t = context.Current;
                            if (t.Type == TokenType.Number)
                            {
                                op.Value.Add(decimal.Parse(t.GetValue()));
                                continue;
                            }
                        }
                    }
                    else if (tv.Equals(")"))
                    {
                        context.MoveNext();
                        hasEnd = true;
                        break;
                    }
                }

                break;
            }

            return hasEnd;
        }

        private static bool ConvertLike(StatementParserContext context, ref Token c, ReadOnlySpan<char> v)
        {
            var s = v.ToString();
            var op = new OperaterStatement();

            if (context.MoveNext() && context.Stack.Peek() is ValueStatement vs)
            {
                var index = context.Index;
                op.Left = vs;
                context.Stack.Pop();
                context.Stack.Push(op);
                context.Parse(context);
                if (context.Stack.TryPop(out var vsss) && vsss is StringValueStatement vss)
                {
                    op.Right = vss;
                    var vv = vss.Value;
                    var start = vv.StartsWith("%");
                    var end = vv.EndsWith("%");
                    if (start && end)
                    {
                        op.Operater = "like";
                        vss.Value = vss.Value[1..^1];
                    }
                    else if (!start && !end)
                    {
                        op.Operater = "like";
                    }
                    else if (start)
                    {
                        op.Operater = "suffix-like";
                        vss.Value = vss.Value[1..];
                    }
                    else
                    {
                        op.Operater = "prefix-like";
                        vss.Value = vss.Value[..^1];
                    }
                    if (context.Stack.Peek() == op)
                    {
                        return true;
                    }
                }
                context.Index = index;
            }
            c = context.Current;
            throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
        }
    }
}
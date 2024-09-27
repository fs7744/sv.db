﻿using SV.Db.Sloth.Statements;

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
                            if (ConvertArrary(context, op) && context.Stack.Peek() == op)
                            {
                                return true;
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
                    else if (v.Equals("or", StringComparison.OrdinalIgnoreCase))
                    {
                        var op = new ConditionsStatement() { Condition = Condition.Or };
                        if (context.MoveNext() && context.Stack.Peek() is ConditionStatement vs)
                        {
                            var index = context.Index;
                            op.Left = vs;
                            context.Stack.Pop();
                            context.Stack.Push(op);
                            context.Parse(context, false);
                            if (context.Stack.TryPop(out var vsss) && vsss is ConditionStatement vss)
                            {
                                op.Right = vss;
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
                    else if (v.Equals("and", StringComparison.OrdinalIgnoreCase))
                    {
                        var op = new ConditionsStatement() { Condition = Condition.And };
                        if (context.MoveNext() && context.Stack.Peek() is ConditionStatement vs)
                        {
                            var index = context.Index;
                            op.Left = vs;
                            context.Stack.Pop();
                            context.Stack.Push(op);
                            context.Parse(context, false);
                            if (context.Stack.TryPop(out var vsss) && vsss is ConditionStatement vss)
                            {
                                op.Right = vss;
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
                }
            }
            return false;
        }

        private bool ConvertArrary(StatementParserContext context, InOperaterStatement iop)
        {
            var t = context.Current;
            if (t.Type == TokenType.Sign
                && t.GetValue().Equals("(", StringComparison.Ordinal)
                && context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Number)
                {
                    if (ConvertNumberArrary(context, t, out var op))
                    {
                        iop.Right = op;
                        return true;
                    }
                }
                else if (t.Type == TokenType.String)
                {
                    if (ConvertStringArrary(context, t, out var op))
                    {
                        iop.Right = op;
                        return true;
                    }
                }
                else if (t.Type == TokenType.True || t.Type == TokenType.False)
                {
                    if (ConvertBoolArrary(context, t, out var op))
                    {
                        iop.Right = op;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ConvertSign(StatementParserContext context, ref Token c)
        {
            var v = c.GetValue();
            var index = context.Index;
            if (v.Equals("<", StringComparison.Ordinal)
                || v.Equals("<=", StringComparison.Ordinal)
                || v.Equals(">", StringComparison.Ordinal)
                || v.Equals(">=", StringComparison.Ordinal)
                || v.Equals("=", StringComparison.Ordinal)
                || v.Equals("!=", StringComparison.Ordinal))
            {
                var op = new OperaterStatement();
                op.Operater = v.ToString();
                if (context.MoveNext() && context.Stack.Peek() is ValueStatement vs)
                {
                    op.Left = vs;
                    context.Stack.Pop();
                    context.Stack.Push(op);
                    context.Parse(context, true);
                    if (context.Stack.TryPop(out var vsss) && vsss is ValueStatement vss)
                    {
                        if (op.Operater == "=" && vss is FieldValueStatement f && f.Field.Equals("null", StringComparison.OrdinalIgnoreCase))
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
            else if (v.Equals("(", StringComparison.Ordinal))
            { 
                
            }
            context.Index = index;
            c = context.Current;
            throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
        }

        private static bool ConvertBoolArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
        {
            var op = new BooleanArrayValueStatement() { Value = new List<bool>() { t.Type == TokenType.True } };
            o = op;
            var hasEnd = false;
            while (context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Sign)
                {
                    var tv = t.GetValue();
                    if (tv.Equals(",", StringComparison.Ordinal))
                    {
                        if (context.MoveNext())
                        {
                            t = context.Current;
                            if (t.Type == TokenType.True)
                            {
                                op.Value.Add(true);
                                continue;
                            }
                            else if (t.Type == TokenType.False)
                            {
                                op.Value.Add(false);
                                continue;
                            }
                        }
                    }
                    else if (tv.Equals(")", StringComparison.Ordinal))
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

        private static bool ConvertStringArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
        {
            var op = new StringArrayValueStatement() { Value = new List<string>() { t.GetValue().ToString() } };
            o = op;
            var hasEnd = false;
            while (context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Sign)
                {
                    var tv = t.GetValue();
                    if (tv.Equals(",", StringComparison.Ordinal))
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
                    else if (tv.Equals(")", StringComparison.Ordinal))
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

        private static bool ConvertNumberArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
        {
            var op = new NumberArrayValueStatement() { Value = new List<decimal>() { decimal.Parse(t.GetValue()) } };
            o = op;
            var hasEnd = false;
            while (context.MoveNext())
            {
                t = context.Current;
                if (t.Type == TokenType.Sign)
                {
                    var tv = t.GetValue();
                    if (tv.Equals(",", StringComparison.Ordinal))
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
                    else if (tv.Equals(")", StringComparison.Ordinal))
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
                context.Parse(context, true);
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
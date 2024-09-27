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
                    var v = c.GetValue();
                    var s = v.ToString();
                    var op = new OperaterStatement();
                    switch (s)
                    {
                        case "<":
                        case "<=":
                        case ">":
                        case ">=":
                        case "=":
                        case "!=":
                            op.Operater = s;
                            if (context.MoveNext() && context.Stack.Peek() is ValueStatement vs)
                            {
                                var index = context.Index;
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
                                context.Index = index;
                            }
                            c = context.Current;
                            throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");

                        default:
                            break;
                    }
                }
                else if (c.Type == TokenType.Word)
                {
                    var v = c.GetValue();
                    if (v.Equals("like", StringComparison.OrdinalIgnoreCase))
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
            return false;
        }
    }
}
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
                                    op.Right = vss;
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
            }
            return false;
        }
    }
}
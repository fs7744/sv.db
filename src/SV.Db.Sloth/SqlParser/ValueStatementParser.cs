using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public class ValueStatementParser : IStatementParser
    {
        public bool TryParse(StatementParserContext context)
        {
            if (context.HasToken()) 
            {
                var c = context.Current;
                switch (c.Type)
                {
                    case TokenType.Number:
                        context.Stack.Push(new NumberValueStatement() { Value = decimal.Parse(c.GetValue()) });
                        context.MoveNext();
                        return true;
                    case TokenType.Word:
                        var v = c.GetValue();
                        if (bool.TryParse(v, out var s))
                        {
                            context.Stack.Push(new BooleanValueStatement() { Value = s });
                        }
                        else
                        {
                            context.Stack.Push(new FieldValueStatement() { Field = v.ToString() });
                        }
                        context.MoveNext();
                        return true;
                    case TokenType.String:
                        context.Stack.Push(new StringValueStatement() { Value = c.GetValue().ToString() });
                        context.MoveNext();
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }
    }
}
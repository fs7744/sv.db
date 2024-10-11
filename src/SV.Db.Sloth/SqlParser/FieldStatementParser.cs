using SV.Db.Sloth.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV.Db.Sloth.SqlParser
{
    public class FieldStatementParser : IStatementParser
    {
        public bool TryParse(StatementParserContext context)
        {
            if (context.HasToken())
            {
                var c = context.Current;
                if (c.Type == TokenType.Word)
                {
                    var v = c.GetValue();
                    if (OperaterStatementParser.TryConvertJsonField(v, context, c))
                    {
                        SkipComma(context);
                        return true;
                    }
                    else
                    {
                        context.Stack.Push(new FieldStatement() { Field = v.ToString() });
                        context.MoveNext();
                        SkipComma(context);
                        return true;
                    }
                }
                else if (c.Type == TokenType.Sign && c.GetValue().Equals("*", StringComparison.Ordinal))
                {
                    context.Stack.Push(new FieldStatement() { Field = "*" });
                    context.MoveNext();
                    SkipComma(context);
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
    }
}
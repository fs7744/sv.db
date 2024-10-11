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
                    if (OperaterStatementParser.TryConvertJsonField(v, context, c))
                    {
                        context.State = StatementState.Fields;
                        return true;
                    }
                    else
                    {
                        if (context.ParseType == ParseType.OrderByField)
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
                else if (context.ParseType == ParseType.SelectField && c.Type == TokenType.Sign && c.GetValue().Equals("*", StringComparison.Ordinal))
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
    }
}
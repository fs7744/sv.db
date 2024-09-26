using SV.Db.Sloth.Statements;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV.Db.Sloth.SqlParser
{
    public static class SqlStatementParser
    {
        private static readonly ITokenParser[] parsers;

        static SqlStatementParser()
        {
            parsers = new ITokenParser[] { new IngoreTokenParser(), new NumberTokenParser(), new WordTokenParser() };
        }

        public static ConditionStatement ParseWhereConditionStatement(string sql)
        {
            var tokens = ParseTokens(sql);
            return null;
        }

        public static IEnumerable<Token> ParseTokens(string sql)
        {
            var context = new ParserContext(sql);
            return Tokenize(context);
        }

        public static IEnumerable<Token> Tokenize(ParserContext context)
        {
            while (context.TryPeek(out var character))
            {
                bool matched = false;
                foreach (var parser in parsers)
                {
                    if (parser.TryTokenize(context, out var t))
                    {
                        matched = true;
                        if (t != null)
                            yield return t;
                    }
                }
                if (!matched)
                    throw new ParserExecption($"Can't parse near by {context.GetSomeChars()} (Line:{context.Line},Col:{context.Column})");
                if (!context.HasNext) { break; }
            }
        }
    }
}
using SV.Db.Sloth.Statements;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SV.Db.Sloth.SqlParser
{
    public static class SqlStatementParser
    {
        private static readonly ITokenParser[] tokenParsers;
        private static readonly IStatementParser[] statementParsers;

        static SqlStatementParser()
        {
            statementParsers = new IStatementParser[] { };
            tokenParsers = new ITokenParser[] { new IngoreTokenParser(), new StringTokenParser(), new NumberTokenParser(), new WordTokenParser(), new SignTokenParser() };
        }

        public static ConditionStatement ParseWhereConditionStatement(string sql)
        {
            return null;
        }

        public static IEnumerable<Statement> ParseStatements(string sql)
        {
            var context = new StatementParserContext(Tokenize(sql).ToArray());
            while (context.HasToken())
            {
                foreach (var parser in statementParsers)
                {
                    if (parser.TryParse(context))
                    {
                        break;
                    }
                }
            }
            return context.Stack;
        }

        public static IEnumerable<Token> Tokenize(string sql)
        {
            var context = new TokenParserContext(sql);
            while (context.TryPeek(out var character))
            {
                bool matched = false;
                foreach (var parser in tokenParsers)
                {
                    if (parser.TryTokenize(context, out var t))
                    {
                        matched = true;
                        if (t != null)
                            yield return t;
                        break;
                    }
                }
                if (!matched)
                    throw new ParserExecption($"Can't parse near by {context.GetSomeChars()} (Line:{context.Line},Col:{context.Column})");
                if (!context.HasNext) { break; }
            }
        }
    }
}
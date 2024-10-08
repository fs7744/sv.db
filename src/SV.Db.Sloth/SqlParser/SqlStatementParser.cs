﻿using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public static class SqlStatementParser
    {
        private static readonly ITokenParser[] tokenParsers;
        private static readonly IStatementParser[] statementParsers;

        static SqlStatementParser()
        {
            statementParsers = new IStatementParser[] { new OperaterStatementParser(), new ValueStatementParser() };
            tokenParsers = new ITokenParser[] { new IngoreTokenParser(), new StringTokenParser(), new NumberTokenParser(), new WordTokenParser(), new SignTokenParser() };
        }

        public static IEnumerable<Statement> ParseStatements(string sql)
        {
            var context = new StatementParserContext(Tokenize(sql).ToArray(), ParseStatements);
            ParseStatements(context, false);
            return context.Stack;
        }

        private static void ParseStatements(StatementParserContext context, bool doOnce)
        {
            while (context.HasToken())
            {
                bool matched = false;
                foreach (var parser in statementParsers)
                {
                    if (parser.TryParse(context))
                    {
                        matched = true;
                        break;
                    }
                }
                if (doOnce) break;
                if (!matched && context.HasToken())
                {
                    var c = context.Current;
                    throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                }
            }
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
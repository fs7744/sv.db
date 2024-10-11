using SV.Db.Sloth.Statements;
using System.Linq;

namespace SV.Db.Sloth.SqlParser
{
    public static class SqlStatementParser
    {
        private static readonly ITokenParser[] tokenParsers;
        private static readonly IStatementParser[] fieldParsers;
        private static readonly IStatementParser[] statementParsers;

        static SqlStatementParser()
        {
            fieldParsers = new IStatementParser[] { new FieldStatementParser() };
            statementParsers = new IStatementParser[] { new OperaterStatementParser(), new ValueStatementParser() };
            tokenParsers = new ITokenParser[] { new IngoreTokenParser(), new StringTokenParser(), new NumberTokenParser(), new WordTokenParser(), new SignTokenParser() };
        }

        public static IEnumerable<Statement> ParseStatements(string sql)
        {
            var context = new StatementParserContext(Tokenize(sql).ToArray(), ParseStatements, false);
            ParseStatements(context, false);
            return context.Stack;
        }

        public static IEnumerable<FieldStatement> ParseFields(string sql)
        {
            var context = new StatementParserContext(Tokenize(sql).ToArray(), ParseStatements, true);
            ParseStatements(context, false);
            return context.Stack.Cast<FieldStatement>();
        }

        private static void ParseStatements(StatementParserContext context, bool doOnce)
        {
            var s = context.ParseField ? fieldParsers : statementParsers;
            while (context.HasToken())
            {
                bool matched = false;
                foreach (var parser in s)
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
using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public class StatementParserContext
    {
        public StatementParserContext(Token[] tokens)
        {
            Stack = new Stack<Statement>();
            Tokens = tokens;
        }

        public Token[] Tokens { get; }

        public Stack<Statement> Stack { get; }

        public int Index { get; }

        public bool HasToken()
        {
            return Index < Tokens.Length;
        }
    }
}
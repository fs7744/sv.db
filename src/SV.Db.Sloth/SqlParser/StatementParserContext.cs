using SV.Db.Sloth.Statements;
using System.Collections;

namespace SV.Db.Sloth.SqlParser
{
    public class StatementParserContext : IEnumerator<Token>
    {
        public StatementParserContext(Token[] tokens, Action<StatementParserContext> parser)
        {
            Stack = new Stack<Statement>();
            Tokens = tokens;
            Parse = parser;
        }

        public Token[] Tokens { get; }
        public Action<StatementParserContext> Parse { get; }
        public Stack<Statement> Stack { get; }

        public int Index { get; set; }

        public Token Current => Tokens[Index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool HasToken()
        {
            return Index < Tokens.Length;
        }

        public bool MoveNext()
        {
            if (HasToken())
            {
                Index++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
        }
    }
}
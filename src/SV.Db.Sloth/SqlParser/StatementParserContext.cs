using SV.Db.Sloth.Statements;
using System.Collections;

namespace SV.Db.Sloth.SqlParser
{
    public class StatementParserContext : IEnumerator<Token>
    {
        public StatementParserContext(Token[] tokens)
        {
            Stack = new Stack<Statement>();
            Tokens = tokens;
        }

        public Token[] Tokens { get; }

        public Stack<Statement> Stack { get; }

        public int Index { get; private set; }

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
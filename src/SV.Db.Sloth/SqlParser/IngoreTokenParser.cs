using SV.Db.Sloth.Statements;
using System.Collections.Frozen;

namespace SV.Db.Sloth.SqlParser
{
    public class IngoreTokenParser : ITokenParser
    {
        private readonly FrozenSet<char> chars = new char[] { Symbols.NewLine, Symbols.Whitespace, Symbols.CarriageReturn, Symbols.Tab, Symbols.EOF }.ToFrozenSet();

        public bool TryTokenize(ParserContext context, out Token t)
        {
            t = null;
            if (context.TryPeek(out var c) && chars.Contains(c))
            {
                return context.TryNext(out c);
            }
            return false;
        }
    }
}
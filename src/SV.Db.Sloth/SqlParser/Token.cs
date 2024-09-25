

namespace SV.Db.Sloth.SqlParser
{

    public class Token
    {
        public TokenType Type { get; set; }
        public ParserContext Context { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public int StartIndex { get; set; }
        public int Count { get; set; }

        public void Reset()
        {
            Context.Reset(this);
        }

        public void End()
        {
            EndLine = Context.Line;
            EndColumn = Context.Column;
        }

        public static Token New(ParserContext context)
        {
            return new Token()
            {
                Context = context,
                StartLine = context.Line,
                StartColumn = context.Column,
                StartIndex = context.Index,
            };
        }

        public ReadOnlySpan<char> GetValue()
        {
            return Context.Data.AsSpan(StartIndex, Count);
        }

        public override string ToString()
        {
            return $"{Enums<TokenType>.GetName(Type)},{GetValue()}";
        }
    }
}
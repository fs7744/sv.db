namespace SV.Db.Sloth.SqlParser
{
    public class WordTokenParser : ITokenParser
    {
        public bool TryTokenize(ParserContext context, out Token t)
        {
            if (context.TryPeek(out var c))
            {
                t = Token.New(context);
                t.Type = TokenType.Word;
                if (TryParseWord(t))
                {
                    return true;
                }
                t.Reset();
            }
            t = null;
            return false;
        }

        private bool TryParseWord(Token t)
        {
            var context = t.Context;
            while (context.TryPeek(out var c) && !IngoreTokenParser.chars.Contains(c))
            {
                context.TryNext(out c);
            }
            t.Count = context.Index - t.StartIndex;
            return t.Count > 0;
        }
    }

    //public class KeywordTokenParser : ITokenParser
    //{
    //    public KeywordTokenParser(params (string keyword, bool)[] keywords)
    //    {
    //        this.keywords = keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    //        this.lens = keywords.Select(c => c.Length).Distinct().Order().ToArray();
    //        this.maxLen = this.lens.Max();
    //    }

    //    private readonly FrozenSet<string> keywords;
    //    private readonly int[] lens;
    //    private readonly int maxLen;

    //    public bool TryTokenize(ParserContext context, out Token t)
    //    {
    //        if (context.TryPeek(out var c))
    //        {
    //            t = Token.New(context);
    //            t.Type = TokenType.Keyword;
    //            if (TryParseKeyword(t))
    //            {
    //                return true;
    //            }
    //            t.Reset();
    //        }
    //        t = null;
    //        return false;
    //    }

    //    private bool TryParseKeyword(Token t)
    //    {
    //    }
    //}
}
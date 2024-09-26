﻿using System.Collections.Frozen;

namespace SV.Db.Sloth.SqlParser
{
    public class IngoreTokenParser : ITokenParser
    {
        internal static readonly FrozenSet<char> chars = new char[] { Symbols.NewLine, Symbols.Whitespace, Symbols.CarriageReturn, Symbols.Tab, Symbols.EOF }.ToFrozenSet();

        public bool TryTokenize(ParserContext context, out Token t)
        {
            t = null;
            if (context.TryPeek(out var c) && chars.Contains(c))
            {
                context.TryNext(out c);
                return true;
            }
            return false;
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
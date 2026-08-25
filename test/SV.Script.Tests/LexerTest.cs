using SV.Script.Syntax;

namespace SV.Script.Tests;

public class LexerTest
{
    private static (TokenKind[] Kinds, List<Diagnostic> Diags, List<string> Strings) Lex(string src)
    {
        var diags = new List<Diagnostic>();
        var lexer = new Lexer(src, diags);
        var toks = lexer.Tokenize();
        return (toks.Select(t => t.Kind).ToArray(), diags, lexer.StringPool);
    }

    private static TokenKind[] Kinds(string src)
    {
        var r = Lex(src);
        Assert.Empty(r.Diags);
        return r.Kinds;
    }

    // ---------------------------------------------------------------- 基本切分

    [Fact]
    public void EmptySourceIsJustEof()
        => Assert.Equal([TokenKind.Eof], Kinds(""));

    [Fact]
    public void WhitespaceOnlyIsJustEof()
        => Assert.Equal([TokenKind.Eof], Kinds("  \t\r\n  "));

    [Fact]
    public void SimpleExpression()
        => Assert.Equal(
            [TokenKind.Ident, TokenKind.Plus, TokenKind.Number, TokenKind.Semi, TokenKind.Eof],
            Kinds("a + 1;"));

    // ---------------------------------------------------------------- 运算符（含双字符）

    [Theory]
    [InlineData("+", TokenKind.Plus)]
    [InlineData("-", TokenKind.Minus)]
    [InlineData("*", TokenKind.Star)]
    [InlineData("/", TokenKind.Slash)]
    [InlineData("%", TokenKind.Percent)]
    [InlineData("=", TokenKind.Assign)]
    [InlineData("+=", TokenKind.PlusAssign)]
    [InlineData("-=", TokenKind.MinusAssign)]
    [InlineData("*=", TokenKind.StarAssign)]
    [InlineData("/=", TokenKind.SlashAssign)]
    [InlineData("%=", TokenKind.PercentAssign)]
    [InlineData("==", TokenKind.EqEq)]
    [InlineData("!=", TokenKind.NotEq)]
    [InlineData("<", TokenKind.Lt)]
    [InlineData("<=", TokenKind.Le)]
    [InlineData(">", TokenKind.Gt)]
    [InlineData(">=", TokenKind.Ge)]
    [InlineData("!", TokenKind.Bang)]
    [InlineData("&&", TokenKind.AmpAmp)]
    [InlineData("||", TokenKind.PipePipe)]
    [InlineData("??", TokenKind.QQ)]
    [InlineData("?.", TokenKind.QDot)]
    [InlineData("?", TokenKind.Question)]
    [InlineData(":", TokenKind.Colon)]
    [InlineData(".", TokenKind.Dot)]
    [InlineData(",", TokenKind.Comma)]
    [InlineData(";", TokenKind.Semi)]
    [InlineData("(", TokenKind.LParen)]
    [InlineData(")", TokenKind.RParen)]
    [InlineData("{", TokenKind.LBrace)]
    [InlineData("}", TokenKind.RBrace)]
    [InlineData("[", TokenKind.LBracket)]
    [InlineData("]", TokenKind.RBracket)]
    public void SingleOperator(string src, TokenKind expected)
        => Assert.Equal([expected, TokenKind.Eof], Kinds(src));

    [Fact]
    public void GreedyTwoCharOperators()
        => Assert.Equal(
            [TokenKind.Le, TokenKind.Ge, TokenKind.EqEq, TokenKind.NotEq, TokenKind.Eof],
            Kinds("<= >= == !="));

    [Fact]
    public void QuestionDotVersusQuestionColon()
        => Assert.Equal(
            [TokenKind.QDot, TokenKind.QQ, TokenKind.Question, TokenKind.Eof],
            Kinds("?. ?? ?"));

    // ---------------------------------------------------------------- 关键字

    [Theory]
    [InlineData("let", TokenKind.KwLet)]
    [InlineData("if", TokenKind.KwIf)]
    [InlineData("else", TokenKind.KwElse)]
    [InlineData("while", TokenKind.KwWhile)]
    [InlineData("for", TokenKind.KwFor)]
    [InlineData("foreach", TokenKind.KwForeach)]
    [InlineData("in", TokenKind.KwIn)]
    [InlineData("break", TokenKind.KwBreak)]
    [InlineData("continue", TokenKind.KwContinue)]
    [InlineData("return", TokenKind.KwReturn)]
    [InlineData("true", TokenKind.KwTrue)]
    [InlineData("false", TokenKind.KwFalse)]
    [InlineData("null", TokenKind.KwNull)]
    [InlineData("and", TokenKind.KwAnd)]
    [InlineData("or", TokenKind.KwOr)]
    [InlineData("not", TokenKind.KwNot)]
    public void Keyword(string src, TokenKind expected)
        => Assert.Equal([expected, TokenKind.Eof], Kinds(src));

    [Theory]
    [InlineData("lets")]
    [InlineData("iff")]
    [InlineData("returned")]
    [InlineData("andy")]
    [InlineData("_if")]
    [InlineData("If")]        // 关键字大小写敏感
    [InlineData("TRUE")]
    public void KeywordPrefixIsStillIdentifier(string src)
        => Assert.Equal([TokenKind.Ident, TokenKind.Eof], Kinds(src));

    // ---------------------------------------------------------------- 标识符

    [Theory]
    [InlineData("a")]
    [InlineData("_x")]
    [InlineData("x1")]
    [InlineData("camelCase")]
    [InlineData("PascalCase")]
    [InlineData("with_under_score")]
    [InlineData("数量")]           // 非 ASCII 视为标识符字符
    [InlineData("单价2")]
    [InlineData("Ünïcodé")]
    public void Identifier(string src)
        => Assert.Equal([TokenKind.Ident, TokenKind.Eof], Kinds(src));

    // ---------------------------------------------------------------- 数字

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("123456789")]
    [InlineData("1.5")]
    [InlineData("0.0001")]
    [InlineData("9223372036854775807")]
    public void Number(string src)
        => Assert.Equal([TokenKind.Number, TokenKind.Eof], Kinds(src));

    [Fact]
    public void DotNotFollowedByDigitIsMemberAccess()
        => Assert.Equal(
            [TokenKind.Number, TokenKind.Dot, TokenKind.Ident, TokenKind.LParen, TokenKind.RParen, TokenKind.Eof],
            Kinds("1.ToString()"));

    [Fact]
    public void MinusBeforeNumberIsItsOwnToken()
        => Assert.Equal(
            [TokenKind.Minus, TokenKind.Number, TokenKind.Eof],
            Kinds("-1"));

    [Fact]
    public void SubtractionOfLiteralIsNotSwallowed()
        => Assert.Equal(
            [TokenKind.Number, TokenKind.Minus, TokenKind.Number, TokenKind.Eof],
            Kinds("10-1"));

    [Fact]
    public void NumberFollowedByIdentIsAnError()
    {
        var r = Lex("123abc");
        Assert.Contains(r.Diags, d => d.Message.Contains("数字字面量"));
    }

    // ---------------------------------------------------------------- 字符串

    [Fact]
    public void DoubleQuotedString()
    {
        var r = Lex("\"hi\"");
        Assert.Empty(r.Diags);
        Assert.Equal(["hi"], r.Strings);
    }

    [Fact]
    public void SingleQuotedString()
    {
        var r = Lex("'hi'");
        Assert.Empty(r.Diags);
        Assert.Equal(["hi"], r.Strings);
    }

    [Fact]
    public void EmptyString()
    {
        var r = Lex("\"\"");
        Assert.Empty(r.Diags);
        Assert.Equal([""], r.Strings);
    }

    [Fact]
    public void EscapeSequences()
    {
        var r = Lex("\"a\\nb\\tc\\rd\\\\e\\\"f\"");
        Assert.Empty(r.Diags);
        Assert.Equal(["a\nb\tc\rd\\e\"f"], r.Strings);
    }

    [Fact]
    public void QuoteOfOtherKindNeedsNoEscape()
    {
        var r = Lex("\"it's\"");
        Assert.Empty(r.Diags);
        Assert.Equal(["it's"], r.Strings);
    }

    [Fact]
    public void UnicodeAndEmojiInStrings()
    {
        var r = Lex("\"中文 🚀\"");
        Assert.Empty(r.Diags);
        Assert.Equal(["中文 🚀"], r.Strings);
    }

    [Fact]
    public void UnterminatedStringIsAnError()
        => Assert.Contains(Lex("\"abc").Diags, d => d.Message.Contains("未闭合的字符串"));

    [Fact]
    public void StringCannotSpanLines()
        => Assert.Contains(Lex("\"abc\ndef\"").Diags, d => d.Message.Contains("不能跨行"));

    [Fact]
    public void UnknownEscapeIsAnError()
        => Assert.Contains(Lex("\"a\\qb\"").Diags, d => d.Message.Contains("转义"));

    // ---------------------------------------------------------------- 注释

    [Fact]
    public void LineComment()
        => Assert.Equal([TokenKind.Number, TokenKind.Eof], Kinds("1 // 后面全是注释"));

    [Fact]
    public void LineCommentEndsAtNewline()
        => Assert.Equal([TokenKind.Number, TokenKind.Number, TokenKind.Eof], Kinds("1 // x\n2"));

    [Fact]
    public void BlockComment()
        => Assert.Equal([TokenKind.Number, TokenKind.Number, TokenKind.Eof], Kinds("1 /* 中间\n跨行 */ 2"));

    [Fact]
    public void CommentOnlySource()
        => Assert.Equal([TokenKind.Eof], Kinds("// 只有注释"));

    [Fact]
    public void UnterminatedBlockCommentIsAnError()
        => Assert.Contains(Lex("/* 没有收尾").Diags, d => d.Message.Contains("未闭合的块注释"));

    [Fact]
    public void SlashIsStillDivisionWhenNotAComment()
        => Assert.Equal(
            [TokenKind.Number, TokenKind.Slash, TokenKind.Number, TokenKind.Eof],
            Kinds("6 / 3"));

    // ---------------------------------------------------------------- 位置信息

    [Fact]
    public void LineAndColumnAreTracked()
    {
        var diags = new List<Diagnostic>();
        var toks = new Lexer("a\nbb\n  ccc", diags).Tokenize();
        Assert.Empty(diags);

        Assert.Equal((1, 1), (toks[0].Line, toks[0].Col));
        Assert.Equal((2, 1), (toks[1].Line, toks[1].Col));
        Assert.Equal((3, 3), (toks[2].Line, toks[2].Col));
    }

    [Fact]
    public void CarriageReturnLineFeedIsHandled()
    {
        var diags = new List<Diagnostic>();
        var toks = new Lexer("a\r\nb", diags).Tokenize();
        Assert.Empty(diags);
        Assert.Equal(2, toks[1].Line);
    }

    [Fact]
    public void UnknownCharacterIsAnError()
        => Assert.Contains(Lex("a @ b").Diags, d => d.Message.Contains("无法识别的字符"));

    [Fact]
    public void SingleAmpersandIsAnError()
        => Assert.Contains(Lex("a & b").Diags, d => d.Message.Contains("无法识别的字符"));

    [Fact]
    public void SinglePipeIsAnError()
        => Assert.Contains(Lex("a | b").Diags, d => d.Message.Contains("无法识别的字符"));

    [Fact]
    public void DiagnosticCarriesPosition()
    {
        var d = Lex("let a = 1;\nlet b = @;").Diags.Single();
        Assert.Equal(2, d.Line);
        Assert.Equal(9, d.Col);
        Assert.Equal(DiagSeverity.Error, d.Severity);
    }
}

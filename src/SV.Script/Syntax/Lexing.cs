using System.Text;

namespace SV.Script.Syntax;

public enum TokenKind : byte
{
    Eof, Number, String, Ident,

    // keywords
    KwLet, KwIf, KwElse, KwWhile, KwFor, KwForeach, KwIn,
    KwBreak, KwContinue, KwReturn, KwTrue, KwFalse, KwNull, KwAnd, KwOr, KwNot,

    // operators / punctuation
    Plus, Minus, Star, Slash, Percent,
    Assign, PlusAssign, MinusAssign, StarAssign, SlashAssign, PercentAssign,
    EqEq, NotEq, Lt, Le, Gt, Ge,
    AmpAmp, PipePipe, Bang, QQ, QDot, Question, Colon,
    Dot, Comma, Semi,
    LParen, RParen, LBrace, RBrace, LBracket, RBracket,
}

/// <summary>值类型 token，词法期不产生堆分配（字符串字面量除外）。</summary>
public readonly struct Token
{
    public readonly TokenKind Kind;
    public readonly int Start;
    public readonly int Length;
    public readonly int Line;
    public readonly int Col;

    /// <summary>String token: 字符串池索引。其余 token 未使用。</summary>
    public readonly int Extra;

    public Token(TokenKind kind, int start, int length, int line, int col, int extra = 0)
    {
        Kind = kind; Start = start; Length = length; Line = line; Col = col; Extra = extra;
    }

    public override string ToString() => $"{Kind}@{Line}:{Col}";
}

public enum DiagSeverity : byte { Error, Warning }

public readonly struct Diagnostic
{
    public readonly DiagSeverity Severity;
    public readonly string Message;
    public readonly int Line;
    public readonly int Col;

    public Diagnostic(DiagSeverity severity, string message, int line, int col)
    {
        Severity = severity; Message = message; Line = line; Col = col;
    }

    public override string ToString()
        => $"({Line},{Col}): {(Severity == DiagSeverity.Error ? "error" : "warning")}: {Message}";
}

/// <summary>单遍、无回溯词法分析。字符分类走 ReadOnlySpan&lt;byte&gt; 常量表（数据段引用，无静态初始化）。</summary>
public sealed class Lexer
{
    private const byte C_NONE = 0, C_SPACE = 1, C_DIGIT = 2, C_IDENT = 4, C_PUNCT = 8;

    private static ReadOnlySpan<byte> Table =>
    [
        // 0x00-0x0F
        C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE,
        C_SPACE, C_SPACE, C_SPACE, C_SPACE, C_SPACE, C_NONE, C_NONE,
        // 0x10-0x1F
        C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE,
        C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE, C_NONE,
        // 0x20-0x2F   sp ! " # $ % & ' ( ) * + , - . /
        C_SPACE, C_PUNCT, C_PUNCT, C_NONE, C_NONE, C_PUNCT, C_PUNCT, C_PUNCT,
        C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT,
        // 0x30-0x39   0-9
        C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT, C_DIGIT,
        // 0x3A-0x3F   : ; < = > ?
        C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT, C_PUNCT,
        // 0x40-0x4F   @ A-O
        C_NONE, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        // 0x50-0x5F   P-Z [ \ ] ^ _
        C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        C_IDENT, C_IDENT, C_IDENT, C_PUNCT, C_NONE, C_PUNCT, C_NONE, C_IDENT,
        // 0x60-0x6F   ` a-o
        C_NONE, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        // 0x70-0x7F   p-z { | } ~ del
        C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT, C_IDENT,
        C_IDENT, C_IDENT, C_IDENT, C_PUNCT, C_PUNCT, C_PUNCT, C_NONE, C_NONE,
    ];

    private readonly string _src;
    private readonly List<Diagnostic> _diags;
    private readonly List<string> _strings = new();
    private int _i;
    private int _line = 1;
    private int _lineStart;

    public Lexer(string source, List<Diagnostic> diagnostics)
    {
        _src = source ?? string.Empty;
        _diags = diagnostics;
    }

    public List<string> StringPool => _strings;

    // 非 ASCII 一律视为标识符字符，于是中文变量名可用
    private static byte Cls(char c) => c < 128 ? Table[c] : C_IDENT;

    private static bool IsIdentStart(char c) => (Cls(c) & C_IDENT) != 0;

    private static bool IsIdentPart(char c) => (Cls(c) & (C_IDENT | C_DIGIT)) != 0;

    private char Peek1 => _i + 1 < _src.Length ? _src[_i + 1] : ' ';

    private int Col => _i - _lineStart + 1;

    public Token[] Tokenize()
    {
        var list = new List<Token>(Math.Max(16, _src.Length / 4));
        while (true)
        {
            SkipTrivia();
            if (_i >= _src.Length)
            {
                list.Add(new Token(TokenKind.Eof, _i, 0, _line, Col));
                break;
            }
            list.Add(Scan());
        }
        return list.ToArray();
    }

    private void SkipTrivia()
    {
        while (_i < _src.Length)
        {
            var c = _src[_i];
            if (c == '\n')
            {
                _i++; _line++; _lineStart = _i;
            }
            else if ((Cls(c) & C_SPACE) != 0)
            {
                _i++;
            }
            else if (c == '/' && Peek1 == '/')
            {
                while (_i < _src.Length && _src[_i] != '\n') _i++;
            }
            else if (c == '/' && Peek1 == '*')
            {
                var sl = _line;
                var sc = Col;
                _i += 2;
                while (true)
                {
                    if (_i >= _src.Length) { Error("未闭合的块注释", sl, sc); return; }
                    if (_src[_i] == '\n') { _i++; _line++; _lineStart = _i; continue; }
                    if (_src[_i] == '*' && Peek1 == '/') { _i += 2; break; }
                    _i++;
                }
            }
            else
            {
                break;
            }
        }
    }

    private Token Scan()
    {
        int start = _i, line = _line, col = Col;
        char c = _src[_i];

        if ((Cls(c) & C_DIGIT) != 0) return ScanNumber(start, line, col);
        if (c == '"' || c == '\'') return ScanString(start, line, col);
        if (IsIdentStart(c)) return ScanIdent(start, line, col);

        _i++;
        TokenKind k;
        switch (c)
        {
            case '+': k = Take('=') ? TokenKind.PlusAssign : TokenKind.Plus; break;
            case '-': k = Take('=') ? TokenKind.MinusAssign : TokenKind.Minus; break;
            case '*': k = Take('=') ? TokenKind.StarAssign : TokenKind.Star; break;
            case '/': k = Take('=') ? TokenKind.SlashAssign : TokenKind.Slash; break;
            case '%': k = Take('=') ? TokenKind.PercentAssign : TokenKind.Percent; break;
            case '=': k = Take('=') ? TokenKind.EqEq : TokenKind.Assign; break;
            case '!': k = Take('=') ? TokenKind.NotEq : TokenKind.Bang; break;
            case '<': k = Take('=') ? TokenKind.Le : TokenKind.Lt; break;
            case '>': k = Take('=') ? TokenKind.Ge : TokenKind.Gt; break;
            case '&': k = Take('&') ? TokenKind.AmpAmp : Unknown(c, line, col); break;
            case '|': k = Take('|') ? TokenKind.PipePipe : Unknown(c, line, col); break;
            case '?': k = Take('?') ? TokenKind.QQ : Take('.') ? TokenKind.QDot : TokenKind.Question; break;
            case ':': k = TokenKind.Colon; break;
            case '.': k = TokenKind.Dot; break;
            case ',': k = TokenKind.Comma; break;
            case ';': k = TokenKind.Semi; break;
            case '(': k = TokenKind.LParen; break;
            case ')': k = TokenKind.RParen; break;
            case '{': k = TokenKind.LBrace; break;
            case '}': k = TokenKind.RBrace; break;
            case '[': k = TokenKind.LBracket; break;
            case ']': k = TokenKind.RBracket; break;
            default: k = Unknown(c, line, col); break;
        }
        return new Token(k, start, _i - start, line, col);
    }

    private TokenKind Unknown(char c, int line, int col)
    {
        Error($"无法识别的字符 '{c}'", line, col);
        return TokenKind.Eof;
    }

    private bool Take(char expected)
    {
        if (_i < _src.Length && _src[_i] == expected) { _i++; return true; }
        return false;
    }

    private Token ScanNumber(int start, int line, int col)
    {
        while (_i < _src.Length && (Cls(_src[_i]) & C_DIGIT) != 0) _i++;

        // 小数点后必须紧跟数字，所以 "1.ToString()" 不会被吞成小数
        if (_i < _src.Length && _src[_i] == '.' && (Cls(Peek1) & C_DIGIT) != 0)
        {
            _i++;
            while (_i < _src.Length && (Cls(_src[_i]) & C_DIGIT) != 0) _i++;
        }

        if (_i < _src.Length && IsIdentStart(_src[_i]))
        {
            Error("数字字面量后不能紧跟标识符", line, col);
            while (_i < _src.Length && IsIdentPart(_src[_i])) _i++;
        }
        return new Token(TokenKind.Number, start, _i - start, line, col);
    }

    private Token ScanString(int start, int line, int col)
    {
        char quote = _src[_i++];
        var sb = new StringBuilder();
        while (true)
        {
            if (_i >= _src.Length) { Error("未闭合的字符串字面量", line, col); break; }
            char c = _src[_i];
            if (c == quote) { _i++; break; }
            if (c == '\n') { Error("字符串字面量不能跨行", line, col); break; }
            if (c == '\\')
            {
                _i++;
                if (_i >= _src.Length) { Error("未闭合的转义序列", line, col); break; }
                char e = _src[_i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '0': sb.Append((char)0); break;
                    case '\\': sb.Append('\\'); break;
                    case '\'': sb.Append('\''); break;
                    case '"': sb.Append('"'); break;
                    default:
                        Error($"未知的转义序列 \\{e}", line, col);
                        sb.Append(e);
                        break;
                }
                continue;
            }
            sb.Append(c);
            _i++;
        }
        _strings.Add(sb.ToString());
        return new Token(TokenKind.String, start, _i - start, line, col, _strings.Count - 1);
    }

    private Token ScanIdent(int start, int line, int col)
    {
        while (_i < _src.Length && IsIdentPart(_src[_i])) _i++;
        var span = _src.AsSpan(start, _i - start);
        return new Token(Keyword(span), start, _i - start, line, col);
    }

    /// <summary>按长度分支 + span 比较，识别关键字时不分配字符串。</summary>
    private static TokenKind Keyword(ReadOnlySpan<char> s) => s.Length switch
    {
        2 => s.SequenceEqual("if") ? TokenKind.KwIf
           : s.SequenceEqual("in") ? TokenKind.KwIn
           : s.SequenceEqual("or") ? TokenKind.KwOr
           : TokenKind.Ident,
        3 => s.SequenceEqual("let") ? TokenKind.KwLet
           : s.SequenceEqual("and") ? TokenKind.KwAnd
           : s.SequenceEqual("not") ? TokenKind.KwNot
           : s.SequenceEqual("for") ? TokenKind.KwFor
           : TokenKind.Ident,
        4 => s.SequenceEqual("else") ? TokenKind.KwElse
           : s.SequenceEqual("true") ? TokenKind.KwTrue
           : s.SequenceEqual("null") ? TokenKind.KwNull
           : TokenKind.Ident,
        5 => s.SequenceEqual("while") ? TokenKind.KwWhile
           : s.SequenceEqual("break") ? TokenKind.KwBreak
           : s.SequenceEqual("false") ? TokenKind.KwFalse
           : TokenKind.Ident,
        6 => s.SequenceEqual("return") ? TokenKind.KwReturn : TokenKind.Ident,
        7 => s.SequenceEqual("foreach") ? TokenKind.KwForeach : TokenKind.Ident,
        8 => s.SequenceEqual("continue") ? TokenKind.KwContinue : TokenKind.Ident,
        _ => TokenKind.Ident,
    };

    private void Error(string msg, int line, int col)
        => _diags.Add(new Diagnostic(DiagSeverity.Error, msg, line, col));
}

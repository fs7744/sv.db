using System.Globalization;
using SV.Script.Runtime;

namespace SV.Script.Syntax;

/// <summary>
/// 单遍、无回溯的 Pratt（优先级爬升）语法分析器，产出扁平 AST。
/// 加二元运算符只需在 <see cref="Prec"/> 和 <see cref="BinOpOf"/> 各加一行。
/// </summary>
public sealed class Parser
{
    private const int MaxDiagnostics = 100;

    private readonly Token[] _toks;
    private readonly string _src;
    private readonly List<string> _strPool;
    private readonly List<Diagnostic> _diags;

    private readonly List<Node> _nodes = new(64);
    private readonly List<int> _extra = new(32);
    private readonly List<Value> _consts = new(16);
    private readonly Dictionary<Value, int> _constIndex = new(Value.ExactComparer);
    private readonly List<string> _names = new(16);
    private readonly Dictionary<string, int> _nameIndex = new(StringComparer.Ordinal);

    private int _p;

    public Parser(string source, Token[] tokens, List<string> stringPool, List<Diagnostic> diagnostics)
    {
        _src = source;
        _toks = tokens;
        _strPool = stringPool;
        _diags = diagnostics;
    }

    // ---------------------------------------------------------------- 优先级表

    /// <summary>中缀优先级。0 表示不是中缀运算符。数值越大越紧。</summary>
    private static byte Prec(TokenKind k) => k switch
    {
        TokenKind.Question => 1,                                        // ?:   右结合
        TokenKind.QQ => 2,                                              // ??   右结合
        TokenKind.KwOr or TokenKind.PipePipe => 3,
        TokenKind.KwAnd or TokenKind.AmpAmp => 4,
        TokenKind.EqEq or TokenKind.NotEq => 5,
        TokenKind.Lt or TokenKind.Le or TokenKind.Gt or TokenKind.Ge => 6,
        TokenKind.Plus or TokenKind.Minus => 7,
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 8,
        _ => 0,
    };

    private static BinOp BinOpOf(TokenKind k) => k switch
    {
        TokenKind.Plus => BinOp.Add,
        TokenKind.Minus => BinOp.Sub,
        TokenKind.Star => BinOp.Mul,
        TokenKind.Slash => BinOp.Div,
        TokenKind.Percent => BinOp.Mod,
        TokenKind.EqEq => BinOp.Eq,
        TokenKind.NotEq => BinOp.Ne,
        TokenKind.Lt => BinOp.Lt,
        TokenKind.Le => BinOp.Le,
        TokenKind.Gt => BinOp.Gt,
        _ => BinOp.Ge,
    };

    private static AssignOp? AssignOpOf(TokenKind k) => k switch
    {
        TokenKind.Assign => AssignOp.Set,
        TokenKind.PlusAssign => AssignOp.AddSet,
        TokenKind.MinusAssign => AssignOp.SubSet,
        TokenKind.StarAssign => AssignOp.MulSet,
        TokenKind.SlashAssign => AssignOp.DivSet,
        TokenKind.PercentAssign => AssignOp.ModSet,
        _ => null,
    };

    // ---------------------------------------------------------------- 入口

    public Ast Parse()
    {
        var stmts = new List<int>();
        while (CurKind != TokenKind.Eof)
        {
            int before = _p;
            stmts.Add(ParseStatement());
            if (_p == before) Advance(); // 保证前进，避免错误恢复时死循环
        }

        // start 必须在全部子表达式追加完之后再取，否则会指到别人的槽位
        int start = _extra.Count;
        _extra.AddRange(stmts);
        int root = Emit(NodeKind.Block, 0, start, stmts.Count, -1, Math.Max(0, _toks.Length - 1));

        return new Ast(_nodes.ToArray(), _extra.ToArray(), _consts.ToArray(), _names.ToArray(), _toks, _src, root);
    }

    // ---------------------------------------------------------------- 语句

    private int ParseStatement() => CurKind switch
    {
        TokenKind.LBrace => ParseBlock(),
        TokenKind.KwLet => ParseLet(),
        TokenKind.KwIf => ParseIf(),
        TokenKind.KwWhile => ParseWhile(),
        TokenKind.KwFor => ParseFor(),
        TokenKind.KwForeach => ParseForeach(),
        TokenKind.KwBreak => ParseBreakOrContinue(NodeKind.Break),
        TokenKind.KwContinue => ParseBreakOrContinue(NodeKind.Continue),
        TokenKind.KwReturn => ParseReturn(),
        TokenKind.Semi => ParseEmpty(),
        _ => ParseExprStatement(),
    };

    private int ParseEmpty()
    {
        int tok = _p;
        Advance();
        return Emit(NodeKind.Block, 0, _extra.Count, 0, -1, tok);
    }

    private int ParseBlock()
    {
        int tok = _p;
        Expect(TokenKind.LBrace, "{");
        var stmts = new List<int>();
        while (CurKind is not (TokenKind.RBrace or TokenKind.Eof))
        {
            int before = _p;
            stmts.Add(ParseStatement());
            if (_p == before) Advance();
        }
        Expect(TokenKind.RBrace, "}");

        int start = _extra.Count;
        _extra.AddRange(stmts);
        return Emit(NodeKind.Block, 0, start, stmts.Count, -1, tok);
    }

    private int ParseLet()
    {
        int tok = _p;
        Advance(); // let
        int name = ExpectIdentName();
        int init = -1;
        if (Match(TokenKind.Assign)) init = ParseExpr(1);
        Expect(TokenKind.Semi, ";");
        return Emit(NodeKind.Let, 0, name, init, -1, tok);
    }

    private int ParseIf()
    {
        int tok = _p;
        Advance(); // if
        Expect(TokenKind.LParen, "(");
        int cond = ParseExpr(1);
        Expect(TokenKind.RParen, ")");
        int then = ParseStatement();
        int els = -1;
        if (Match(TokenKind.KwElse)) els = ParseStatement(); // else if 天然递归
        return Emit(NodeKind.If, 0, cond, then, els, tok);
    }

    private int ParseWhile()
    {
        int tok = _p;
        Advance();
        Expect(TokenKind.LParen, "(");
        int cond = ParseExpr(1);
        Expect(TokenKind.RParen, ")");
        int body = ParseStatement();
        return Emit(NodeKind.While, 0, cond, body, -1, tok);
    }

    private int ParseFor()
    {
        int tok = _p;
        Advance();
        Expect(TokenKind.LParen, "(");

        int init = -1;
        if (Match(TokenKind.Semi)) { }
        else if (CurKind == TokenKind.KwLet) init = ParseLet();
        else init = ParseExprStatement();

        int cond = -1;
        if (CurKind != TokenKind.Semi) cond = ParseExpr(1);
        Expect(TokenKind.Semi, ";");

        int step = -1;
        if (CurKind != TokenKind.RParen)
        {
            int e = ParseExprOrAssign();
            step = Emit(NodeKind.ExprStmt, 0, e, -1, -1, tok);
        }
        Expect(TokenKind.RParen, ")");

        int body = ParseStatement();

        int start = _extra.Count;
        _extra.Add(step);
        _extra.Add(body);
        return Emit(NodeKind.For, 0, init, cond, start, tok);
    }

    private int ParseForeach()
    {
        int tok = _p;
        Advance();
        Expect(TokenKind.LParen, "(");
        Match(TokenKind.KwLet); // let 可选
        int name = ExpectIdentName();
        Expect(TokenKind.KwIn, "in");
        int src = ParseExpr(1);
        Expect(TokenKind.RParen, ")");
        int body = ParseStatement();
        return Emit(NodeKind.Foreach, 0, name, src, body, tok);
    }

    private int ParseBreakOrContinue(NodeKind kind)
    {
        int tok = _p;
        Advance();
        Expect(TokenKind.Semi, ";");
        return Emit(kind, 0, -1, -1, -1, tok);
    }

    private int ParseReturn()
    {
        int tok = _p;
        Advance();
        int expr = -1;
        if (CurKind is not (TokenKind.Semi or TokenKind.Eof)) expr = ParseExpr(1);
        Expect(TokenKind.Semi, ";");
        return Emit(NodeKind.Return, 0, expr, -1, -1, tok);
    }

    private int ParseExprStatement()
    {
        int tok = _p;
        int e = ParseExprOrAssign();
        Expect(TokenKind.Semi, ";");
        return Emit(NodeKind.ExprStmt, 0, e, -1, -1, tok);
    }

    /// <summary>表达式，后面可以紧跟赋值运算符。赋值不作为运算符参与优先级，避免 a = b == c 的歧义。</summary>
    private int ParseExprOrAssign()
    {
        int tok = _p;
        int left = ParseExpr(1);
        var op = AssignOpOf(CurKind);
        if (op is null) return left;

        var target = _nodes[left].Kind;
        if (target == NodeKind.SafeChain)
        {
            Error("?. 不能作为赋值目标，请先判空再写入");
        }
        else if (target is not (NodeKind.Ident or NodeKind.Member or NodeKind.Index))
        {
            Error("赋值目标只能是变量、成员或下标");
        }
        Advance();
        int value = ParseExpr(1);
        return Emit(NodeKind.Assign, (byte)op.Value, left, value, -1, tok);
    }

    // ---------------------------------------------------------------- 表达式

    private int ParseExpr(byte minPrec)
    {
        int left = ParseUnary();
        while (true)
        {
            var k = CurKind;
            byte p = Prec(k);
            if (p == 0 || p < minPrec) return left;

            int tok = _p;
            switch (k)
            {
                case TokenKind.Question:
                {
                    Advance();
                    int then = ParseExpr(1);
                    Expect(TokenKind.Colon, ":");
                    int els = ParseExpr(1); // 右结合
                    left = Emit(NodeKind.Conditional, 0, left, then, els, tok);
                    break;
                }
                case TokenKind.QQ:
                {
                    Advance();
                    int right = ParseExpr(p); // 右结合
                    left = Emit(NodeKind.Coalesce, 0, left, right, -1, tok);
                    break;
                }
                case TokenKind.KwOr:
                case TokenKind.PipePipe:
                {
                    Advance();
                    int right = ParseExpr((byte)(p + 1));
                    left = Emit(NodeKind.Logical, 1, left, right, -1, tok);
                    break;
                }
                case TokenKind.KwAnd:
                case TokenKind.AmpAmp:
                {
                    Advance();
                    int right = ParseExpr((byte)(p + 1));
                    left = Emit(NodeKind.Logical, 0, left, right, -1, tok);
                    break;
                }
                default:
                {
                    var op = BinOpOf(k);
                    Advance();
                    int right = ParseExpr((byte)(p + 1));
                    left = Emit(NodeKind.Binary, (byte)op, left, right, -1, tok);
                    break;
                }
            }
        }
    }

    private int ParseUnary()
    {
        int tok = _p;
        switch (CurKind)
        {
            case TokenKind.Minus:
                Advance();
                return Emit(NodeKind.Unary, (byte)UnOp.Neg, ParseUnary(), -1, -1, tok);
            case TokenKind.Bang:
            case TokenKind.KwNot:
                Advance();
                return Emit(NodeKind.Unary, (byte)UnOp.Not, ParseUnary(), -1, -1, tok);
            case TokenKind.Plus:
                Advance(); // 一元 + 无语义，直接丢弃
                return ParseUnary();
            default:
                return ParsePostfix(ParsePrimary());
        }
    }

    /// <summary>后缀链：'.' '?.' '[' '(' 统一在这里处理，所以方法调用不是特殊语法。</summary>
    private int ParsePostfix(int recv)
    {
        bool hasSafe = false;
        while (true)
        {
            int tok = _p;
            switch (CurKind)
            {
                case TokenKind.Dot:
                case TokenKind.QDot:
                {
                    bool cond = CurKind == TokenKind.QDot;
                    hasSafe |= cond;
                    Advance();
                    int name = ExpectIdentName();
                    if (CurKind == TokenKind.LParen)
                    {
                        int target = Emit(NodeKind.Member, (byte)(cond ? 1 : 0), recv, name, -1, tok);
                        recv = ParseCall(target, tok);
                    }
                    else
                    {
                        recv = Emit(NodeKind.Member, (byte)(cond ? 1 : 0), recv, name, -1, tok);
                    }
                    break;
                }
                case TokenKind.LBracket:
                {
                    Advance();
                    int idx = ParseExpr(1);
                    Expect(TokenKind.RBracket, "]");
                    recv = Emit(NodeKind.Index, 0, recv, idx, -1, tok);
                    break;
                }
                case TokenKind.LParen:
                {
                    recv = ParseCall(recv, tok);
                    break;
                }
                default:
                    return hasSafe ? Emit(NodeKind.SafeChain, 0, recv, -1, -1, tok) : recv;
            }
        }
    }

    private int ParseCall(int target, int tok)
    {
        Expect(TokenKind.LParen, "(");
        var args = new List<int>();
        if (CurKind != TokenKind.RParen)
        {
            do
            {
                args.Add(ParseExpr(1));
            }
            while (Match(TokenKind.Comma));
        }
        Expect(TokenKind.RParen, ")");

        int start = _extra.Count;
        _extra.AddRange(args);
        return Emit(NodeKind.Call, 0, target, start, args.Count, tok);
    }

    private int ParsePrimary()
    {
        int tok = _p;
        var t = Cur;
        switch (t.Kind)
        {
            case TokenKind.Number:
                Advance();
                return Emit(NodeKind.Const, 0, Const(ParseNumber(t)), -1, -1, tok);

            case TokenKind.String:
                Advance();
                return Emit(NodeKind.Const, 0, Const(Value.Str(_strPool[t.Extra])), -1, -1, tok);

            case TokenKind.KwTrue:
                Advance();
                return Emit(NodeKind.Const, 0, Const(Value.True), -1, -1, tok);

            case TokenKind.KwFalse:
                Advance();
                return Emit(NodeKind.Const, 0, Const(Value.False), -1, -1, tok);

            case TokenKind.KwNull:
                Advance();
                return Emit(NodeKind.Const, 0, Const(Value.Null), -1, -1, tok);

            case TokenKind.Ident:
                Advance();
                return Emit(NodeKind.Ident, 0, Name(TokenText(t)), -1, -1, tok);

            case TokenKind.LParen:
            {
                Advance();
                int e = ParseExpr(1);
                Expect(TokenKind.RParen, ")");
                return e;
            }

            case TokenKind.LBracket:
                return ParseArrayLit();

            case TokenKind.LBrace:
                return ParseMapLit();

            default:
                Error($"这里需要一个表达式，但遇到了 {Describe(t)}");
                Advance();
                return Emit(NodeKind.Const, 0, Const(Value.Null), -1, -1, tok);
        }
    }

    private int ParseArrayLit()
    {
        int tok = _p;
        Expect(TokenKind.LBracket, "[");
        var items = new List<int>();
        if (CurKind != TokenKind.RBracket)
        {
            do
            {
                if (CurKind == TokenKind.RBracket) break; // 允许尾逗号
                items.Add(ParseExpr(1));
            }
            while (Match(TokenKind.Comma));
        }
        Expect(TokenKind.RBracket, "]");

        int start = _extra.Count;
        _extra.AddRange(items);
        return Emit(NodeKind.ArrayLit, 0, start, items.Count, -1, tok);
    }

    private int ParseMapLit()
    {
        int tok = _p;
        Expect(TokenKind.LBrace, "{");
        var pairs = new List<int>();
        if (CurKind != TokenKind.RBrace)
        {
            do
            {
                if (CurKind == TokenKind.RBrace) break; // 允许尾逗号

                int key;
                if (CurKind == TokenKind.String) { key = Name(_strPool[Cur.Extra]); Advance(); }
                else key = ExpectIdentName();

                Expect(TokenKind.Colon, ":");
                int value = ParseExpr(1);
                pairs.Add(key);
                pairs.Add(value);
            }
            while (Match(TokenKind.Comma));
        }
        Expect(TokenKind.RBrace, "}");

        int start = _extra.Count;
        _extra.AddRange(pairs);
        return Emit(NodeKind.MapLit, 0, start, pairs.Count / 2, -1, tok);
    }

    private Value ParseNumber(Token t)
    {
        var span = _src.AsSpan(t.Start, t.Length);
        if (span.IndexOf('.') < 0 && long.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out var l))
            return Value.Int(l);
        if (decimal.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return Value.Dec(d);
        Error($"无法识别的数字字面量 '{span}'", t);
        return Value.Zero;
    }

    // ---------------------------------------------------------------- 池 / 工具

    private int Emit(NodeKind kind, byte op, int a, int b, int c, int tok)
    {
        _nodes.Add(new Node(kind, op, a, b, c, tok));
        return _nodes.Count - 1;
    }

    private int Const(Value v)
    {
        if (_constIndex.TryGetValue(v, out var i)) return i;
        _consts.Add(v);
        i = _consts.Count - 1;
        _constIndex[v] = i;
        return i;
    }

    private int Name(string s)
    {
        if (_nameIndex.TryGetValue(s, out var i)) return i;
        _names.Add(s);
        i = _names.Count - 1;
        _nameIndex[s] = i;
        return i;
    }

    private string TokenText(Token t) => _src.Substring(t.Start, t.Length);

    private Token Cur => _toks[_p];

    private TokenKind CurKind => _toks[_p].Kind;

    private void Advance()
    {
        if (_p < _toks.Length - 1) _p++;
    }

    private bool Match(TokenKind k)
    {
        if (CurKind == k) { Advance(); return true; }
        return false;
    }

    private bool Expect(TokenKind k, string what)
    {
        if (CurKind == k) { Advance(); return true; }
        Error($"这里需要 '{what}'，但遇到了 {Describe(Cur)}");
        return false;
    }

    private int ExpectIdentName()
    {
        if (CurKind == TokenKind.Ident)
        {
            var n = Name(TokenText(Cur));
            Advance();
            return n;
        }
        Error($"这里需要一个标识符，但遇到了 {Describe(Cur)}");
        return Name("<error>");
    }

    private string Describe(Token t) => t.Kind switch
    {
        TokenKind.Eof => "文件结束",
        TokenKind.Number or TokenKind.Ident => $"'{TokenText(t)}'",
        TokenKind.String => "字符串字面量",
        _ => t.Length > 0 ? $"'{TokenText(t)}'" : t.Kind.ToString(),
    };

    private void Error(string msg) => Error(msg, Cur);

    private void Error(string msg, Token at)
    {
        if (_diags.Count >= MaxDiagnostics) return;
        _diags.Add(new Diagnostic(DiagSeverity.Error, msg, at.Line, at.Col));
    }
}

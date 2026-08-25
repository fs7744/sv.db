using SV.Script.Runtime;

namespace SV.Script.Syntax;

public enum NodeKind : byte
{
    // ---------- 表达式 ----------
    /// <summary>A = 常量池索引</summary>
    Const,

    /// <summary>A = 名字池索引（Compiler 负责解析成槽位或已注册类型）</summary>
    Ident,

    /// <summary>A = Extra 起点, B = 元素个数</summary>
    ArrayLit,

    /// <summary>A = Extra 起点（键名索引、值节点 交替存放）, B = 键值对数</summary>
    MapLit,

    /// <summary>Op = <see cref="BinOp"/>, A = 左, B = 右</summary>
    Binary,

    /// <summary>Op = <see cref="UnOp"/>, A = 操作数</summary>
    Unary,

    /// <summary>Op: 0 = and, 1 = or；A = 左, B = 右（短路）</summary>
    Logical,

    /// <summary>A = 左, B = 右（?? 空合并）</summary>
    Coalesce,

    /// <summary>A = 条件, B = then, C = else</summary>
    Conditional,

    /// <summary>A = 接收者, B = 名字池索引, Op: 0 = '.', 1 = '?.'</summary>
    Member,

    /// <summary>A = 接收者, B = 下标表达式</summary>
    Index,

    /// <summary>A = 目标（Member 或 Ident）, B = Extra 起点, C = 实参个数</summary>
    Call,

    /// <summary>A = 内层链。链中出现过 '?.' 时包一层，用于整条链一起短路。</summary>
    SafeChain,

    /// <summary>Op = <see cref="AssignOp"/>, A = 目标, B = 值</summary>
    Assign,

    // ---------- 语句 ----------
    /// <summary>A = Extra 起点, B = 语句个数</summary>
    Block,

    /// <summary>A = 名字池索引, B = 初值节点（-1 表示无）</summary>
    Let,

    /// <summary>A = 条件, B = then 语句, C = else 语句（-1 表示无）</summary>
    If,

    /// <summary>A = 条件, B = 循环体</summary>
    While,

    /// <summary>A = init(-1), B = cond(-1), C = Extra 起点 -&gt; [step(-1), body]</summary>
    For,

    /// <summary>A = 变量名索引, B = 可迭代表达式, C = 循环体</summary>
    Foreach,

    Break,
    Continue,

    /// <summary>A = 返回值表达式（-1 表示 return;）</summary>
    Return,

    /// <summary>A = 表达式</summary>
    ExprStmt,
}

public enum BinOp : byte { Add, Sub, Mul, Div, Mod, Eq, Ne, Lt, Le, Gt, Ge }

public enum UnOp : byte { Neg, Not }

public enum AssignOp : byte { Set, AddSet, SubSet, MulSet, DivSet, ModSet }

/// <summary>扁平 AST 的节点。子节点用 int 索引引用，整棵树就是一个数组，无对象图、无 GC 压力。</summary>
public readonly struct Node
{
    public readonly NodeKind Kind;
    public readonly byte Op;
    public readonly int A;
    public readonly int B;
    public readonly int C;

    /// <summary>token 索引，仅用于诊断和行号映射。</summary>
    public readonly int Tok;

    public Node(NodeKind kind, byte op, int a, int b, int c, int tok)
    {
        Kind = kind; Op = op; A = a; B = b; C = c; Tok = tok;
    }

    public override string ToString() => $"{Kind}({Op}) A={A} B={B} C={C}";
}

public sealed class Ast
{
    public Ast(Node[] nodes, int[] extra, Value[] consts, string[] names, Token[] tokens, string source, int root)
    {
        Nodes = nodes; Extra = extra; Consts = consts; Names = names; Tokens = tokens; Source = source; Root = root;
    }

    public Node[] Nodes { get; }

    /// <summary>变长子节点池：数组元素、实参、语句列表、map 键值。</summary>
    public int[] Extra { get; }

    public Value[] Consts { get; }

    public string[] Names { get; }

    public Token[] Tokens { get; }

    public string Source { get; }

    /// <summary>顶层 Block 节点索引。</summary>
    public int Root { get; }
}

using SV.Script.Runtime;
using SV.Script.Syntax;

namespace SV.Script.Emit;

/// <summary>
/// 扁平 AST -&gt; 字节码。单遍完成作用域解析与槽位分配（T1 没有用户自定义函数，所以只有一个帧）。
/// </summary>
public sealed class Compiler
{
    private readonly Ast _ast;
    private readonly Node[] _nodes;
    private readonly int[] _extra;
    private readonly string[] _names;
    private readonly IReadOnlyDictionary<string, Type> _registeredTypes;
    private readonly List<Diagnostic> _diags;
    private readonly bool _strictVariables;

    private readonly List<Instr> _code = new(64);
    private readonly List<int> _lines = new(64);
    private readonly List<int> _cols = new(64);

    private readonly List<Type> _types = new();
    private readonly Dictionary<Type, int> _typeIndex = new();
    private readonly List<MemberSite> _sites = new();
    private readonly List<string[]> _mapKeySets = new();

    /// <summary>可见的块作用域变量。槽位单调分配，不跨兄弟块复用（脚本规模小，浪费可忽略）。</summary>
    private readonly List<(string Name, int Slot)> _visible = new();

    private int _slotCount;

    private readonly Dictionary<string, int> _externalSlots = new(StringComparer.Ordinal);
    private readonly List<string> _externalNames = new();

    /// <summary>每层块作用域在 _visible 里的起始位置，把重复声明检查限定在当前作用域内。</summary>
    private readonly Stack<int> _scopeMarks = new();

    private readonly Stack<LoopCtx> _loops = new();
    private readonly Stack<List<int>> _safeChains = new();

    private int _stack;
    private int _maxStack;

    private sealed class LoopCtx
    {
        public readonly List<int> BreakJumps = new();
        public readonly List<int> ContinueJumps = new();
    }

    public Compiler(Ast ast, IReadOnlyDictionary<string, Type> registeredTypes, List<Diagnostic> diagnostics,
        bool strictVariables)
    {
        _ast = ast;
        _nodes = ast.Nodes;
        _extra = ast.Extra;
        _names = ast.Names;
        _registeredTypes = registeredTypes;
        _diags = diagnostics;
        _strictVariables = strictVariables;
    }

    public ScriptProgram Compile()
    {
        Stmt(_ast.Root);
        Emit(OpCode.ReturnNull, tok: _nodes[_ast.Root].Tok);

        return new ScriptProgram
        {
            Code = _code.ToArray(),
            Consts = _ast.Consts,
            Types = _types.ToArray(),
            Sites = _sites.ToArray(),
            MapKeySets = _mapKeySets.ToArray(),
            SlotCount = _slotCount,
            MaxStack = _maxStack + 8, // 分支处的深度估算留出余量
            MaxArgs = _sites.Count == 0 ? 0 : _sites.Max(x => x.ArgCount),
            ExternalNames = _externalNames.ToArray(),
            Lines = _lines.ToArray(),
            Cols = _cols.ToArray(),
            Source = _ast.Source,
        };
    }

    /// <summary>外部变量名 -&gt; 槽位。宿主按这个表填充实参。</summary>
    public IReadOnlyDictionary<string, int> ExternalSlots => _externalSlots;

    // ================================================================ 语句

    private void Stmt(int n)
    {
        var nd = _nodes[n];
        switch (nd.Kind)
        {
            case NodeKind.Block:
            {
                int mark = EnterScope();
                for (int i = 0; i < nd.B; i++) Stmt(_extra[nd.A + i]);
                ExitScope(mark);
                break;
            }

            case NodeKind.Let:
            {
                // 先编译初值再声明，于是 let x = x; 里的右侧 x 指向外层变量
                if (nd.B >= 0) Expr(nd.B);
                else Emit(OpCode.PushNull, tok: nd.Tok);

                var name = _names[nd.A];
                if (DeclaredInCurrentScope(name))
                    Error($"变量 '{name}' 在当前作用域已声明", nd);

                int slot = NewSlot(name);
                Emit(OpCode.StoreLocal, slot, tok: nd.Tok);
                break;
            }

            case NodeKind.If:
            {
                Expr(nd.A);
                int toElse = Emit(OpCode.JumpIfFalse, 0, tok: nd.Tok);
                Stmt(nd.B);
                if (nd.C >= 0)
                {
                    int toEnd = Emit(OpCode.Jump, 0, tok: nd.Tok);
                    Patch(toElse, _code.Count);
                    Stmt(nd.C);
                    Patch(toEnd, _code.Count);
                }
                else
                {
                    Patch(toElse, _code.Count);
                }
                break;
            }

            case NodeKind.While:
            {
                int top = _code.Count;
                Expr(nd.A);
                int toEnd = Emit(OpCode.JumpIfFalse, 0, tok: nd.Tok);

                var ctx = new LoopCtx();
                _loops.Push(ctx);
                Stmt(nd.B);
                _loops.Pop();

                Emit(OpCode.Jump, top, tok: nd.Tok);
                Patch(toEnd, _code.Count);
                foreach (var j in ctx.BreakJumps) Patch(j, _code.Count);
                foreach (var j in ctx.ContinueJumps) Patch(j, top);
                break;
            }

            case NodeKind.For:
            {
                int mark = EnterScope();
                if (nd.A >= 0) Stmt(nd.A);

                int step = _extra[nd.C];
                int body = _extra[nd.C + 1];

                int top = _code.Count;
                int toEnd = -1;
                if (nd.B >= 0)
                {
                    Expr(nd.B);
                    toEnd = Emit(OpCode.JumpIfFalse, 0, tok: nd.Tok);
                }

                var ctx = new LoopCtx();
                _loops.Push(ctx);
                Stmt(body);
                _loops.Pop();

                int stepIp = _code.Count;
                if (step >= 0) Stmt(step);
                Emit(OpCode.Jump, top, tok: nd.Tok);

                if (toEnd >= 0) Patch(toEnd, _code.Count);
                foreach (var j in ctx.BreakJumps) Patch(j, _code.Count);
                foreach (var j in ctx.ContinueJumps) Patch(j, stepIp);

                ExitScope(mark);
                break;
            }

            case NodeKind.Foreach:
            {
                int mark = EnterScope();
                Expr(nd.B);

                int iter = _slotCount++; // 存放枚举器的隐藏槽位
                Emit(OpCode.IterInit, iter, tok: nd.Tok);

                int loopVar = NewSlot(_names[nd.A]);

                int top = _code.Count;
                int next = Emit(OpCode.IterNext, iter, 0, nd.Tok);
                Emit(OpCode.StoreLocal, loopVar, tok: nd.Tok);

                var ctx = new LoopCtx();
                _loops.Push(ctx);
                Stmt(nd.C);
                _loops.Pop();

                Emit(OpCode.Jump, top, tok: nd.Tok);
                PatchB(next, _code.Count);
                foreach (var j in ctx.BreakJumps) Patch(j, _code.Count);
                foreach (var j in ctx.ContinueJumps) Patch(j, top);

                ExitScope(mark);
                break;
            }

            case NodeKind.Break:
            {
                if (_loops.Count == 0) { Error("break 只能出现在循环内", nd); break; }
                _loops.Peek().BreakJumps.Add(Emit(OpCode.Jump, 0, tok: nd.Tok));
                break;
            }

            case NodeKind.Continue:
            {
                if (_loops.Count == 0) { Error("continue 只能出现在循环内", nd); break; }
                _loops.Peek().ContinueJumps.Add(Emit(OpCode.Jump, 0, tok: nd.Tok));
                break;
            }

            case NodeKind.Return:
            {
                if (nd.A >= 0)
                {
                    Expr(nd.A);
                    Emit(OpCode.Return, tok: nd.Tok);
                }
                else
                {
                    Emit(OpCode.ReturnNull, tok: nd.Tok);
                }
                break;
            }

            case NodeKind.ExprStmt:
            {
                var inner = _nodes[nd.A];
                if (inner.Kind == NodeKind.Assign)
                {
                    Assign(nd.A);          // 赋值不留值在栈上
                }
                else
                {
                    Expr(nd.A);
                    Emit(OpCode.Pop, tok: nd.Tok);
                }
                break;
            }

            default:
                Error($"这里不能作为语句使用 ({nd.Kind})", nd);
                break;
        }
    }

    // ================================================================ 表达式

    private void Expr(int n)
    {
        var nd = _nodes[n];
        switch (nd.Kind)
        {
            case NodeKind.Const:
            {
                var v = _ast.Consts[nd.A];
                if (v.Kind == ValueKind.Int && v.AsInt >= int.MinValue && v.AsInt <= int.MaxValue)
                    Emit(OpCode.PushInt, (int)v.AsInt, tok: nd.Tok);
                else
                    Emit(OpCode.PushConst, nd.A, tok: nd.Tok);
                break;
            }

            case NodeKind.Ident:
            {
                var name = _names[nd.A];
                var slot = Find(name);
                if (slot is not null)
                {
                    Emit(OpCode.LoadLocal, slot.Value, tok: nd.Tok);
                }
                else if (_registeredTypes.TryGetValue(name, out var t))
                {
                    Emit(OpCode.PushType, TypeIdx(t), tok: nd.Tok);
                }
                else if (_strictVariables)
                {
                    Error($"未定义的变量 '{name}'", nd);
                    Emit(OpCode.PushNull, tok: nd.Tok);
                }
                else
                {
                    Emit(OpCode.LoadLocal, External(name), tok: nd.Tok);
                }
                break;
            }

            case NodeKind.ArrayLit:
            {
                for (int i = 0; i < nd.B; i++) Expr(_extra[nd.A + i]);
                Emit(OpCode.NewArray, nd.B, tok: nd.Tok);
                break;
            }

            case NodeKind.MapLit:
            {
                var keys = new string[nd.B];
                for (int i = 0; i < nd.B; i++)
                {
                    keys[i] = _names[_extra[nd.A + i * 2]];
                    Expr(_extra[nd.A + i * 2 + 1]);
                }
                _mapKeySets.Add(keys);
                Emit(OpCode.NewMap, _mapKeySets.Count - 1, nd.B, nd.Tok);
                break;
            }

            case NodeKind.Binary:
            {
                Expr(nd.A);
                Expr(nd.B);
                Emit((BinOp)nd.Op switch
                {
                    BinOp.Add => OpCode.Add,
                    BinOp.Sub => OpCode.Sub,
                    BinOp.Mul => OpCode.Mul,
                    BinOp.Div => OpCode.Div,
                    BinOp.Mod => OpCode.Mod,
                    BinOp.Eq => OpCode.Eq,
                    BinOp.Ne => OpCode.Ne,
                    BinOp.Lt => OpCode.Lt,
                    BinOp.Le => OpCode.Le,
                    BinOp.Gt => OpCode.Gt,
                    _ => OpCode.Ge,
                }, tok: nd.Tok);
                break;
            }

            case NodeKind.Unary:
            {
                Expr(nd.A);
                Emit((UnOp)nd.Op == UnOp.Neg ? OpCode.Neg : OpCode.Not, tok: nd.Tok);
                break;
            }

            case NodeKind.Logical:
            {
                Expr(nd.A);
                int j = Emit(nd.Op == 0 ? OpCode.JumpIfFalseKeep : OpCode.JumpIfTrueKeep, 0, tok: nd.Tok);
                Emit(OpCode.Pop, tok: nd.Tok);
                Expr(nd.B);
                // 右侧也要求 bool，否则 (true and 1) 会返回 1，结果类型随取值而变
                Emit(OpCode.AssertBool, nd.Op, tok: nd.Tok);
                Patch(j, _code.Count);
                break;
            }

            case NodeKind.Coalesce:
            {
                Expr(nd.A);
                int j = Emit(OpCode.JumpIfNotNull, 0, tok: nd.Tok);
                Emit(OpCode.Pop, tok: nd.Tok);
                Expr(nd.B);
                Patch(j, _code.Count);
                break;
            }

            case NodeKind.Conditional:
            {
                Expr(nd.A);
                int toElse = Emit(OpCode.JumpIfFalse, 0, tok: nd.Tok);
                Expr(nd.B);
                int toEnd = Emit(OpCode.Jump, 0, tok: nd.Tok);
                Patch(toElse, _code.Count);
                Expr(nd.C);
                Patch(toEnd, _code.Count);
                break;
            }

            case NodeKind.SafeChain:
            {
                _safeChains.Push(new List<int>());
                Expr(nd.A);
                foreach (var j in _safeChains.Pop()) Patch(j, _code.Count);
                break;
            }

            case NodeKind.Member:
            {
                Expr(nd.A);
                if (nd.Op == 1) GuardNull(nd);
                Emit(OpCode.GetMember, SiteIdx(SiteKind.GetMember, _names[nd.B], 0), tok: nd.Tok);
                break;
            }

            case NodeKind.Index:
            {
                Expr(nd.A);
                Expr(nd.B);
                Emit(OpCode.GetIndex, tok: nd.Tok);
                break;
            }

            case NodeKind.Call:
                Call(n, nd);
                break;

            case NodeKind.Assign:
                // 赋值出现在表达式位置（比如 for 的步进），编译后不留值
                Assign(n);
                Emit(OpCode.PushNull, tok: nd.Tok);
                break;

            default:
                Error($"这里不能作为表达式使用 ({nd.Kind})", nd);
                Emit(OpCode.PushNull, tok: nd.Tok);
                break;
        }
    }

    private void Call(int n, Node nd)
    {
        var target = _nodes[nd.A];

        if (target.Kind == NodeKind.Member)
        {
            Expr(target.A);                                  // 接收者
            if (target.Op == 1) GuardNull(nd);               // obj?.M(...) 整链短路
            for (int i = 0; i < nd.C; i++) Expr(_extra[nd.B + i]);
            Emit(OpCode.Call, SiteIdx(SiteKind.Call, _names[target.B], nd.C), tok: nd.Tok);
            return;
        }

        if (target.Kind == NodeKind.Ident)
        {
            var name = _names[target.A];
            Error(_registeredTypes.ContainsKey(name)
                ? $"'{name}' 是类型，不能直接调用；请写成 {name}.方法名(...)"
                : $"未定义的函数 '{name}'（本版本不支持脚本内定义函数，请用 宿主类型.方法(...)）", nd);
        }
        else
        {
            Error("只能调用成员方法，例如 obj.Method(...) 或 Type.Method(...)", nd);
        }
        Emit(OpCode.PushNull, tok: nd.Tok);
    }

    private void Assign(int n)
    {
        var nd = _nodes[n];
        var op = (AssignOp)nd.Op;
        var target = _nodes[nd.A];

        switch (target.Kind)
        {
            case NodeKind.Ident:
            {
                var name = _names[target.A];
                var slot = Find(name);
                if (slot is null)
                {
                    if (_registeredTypes.ContainsKey(name)) { Error($"不能给类型 '{name}' 赋值", nd); return; }
                    if (_strictVariables) { Error($"未定义的变量 '{name}'，请先用 let 声明", nd); return; }
                    slot = External(name);
                }
                if (op != AssignOp.Set) Emit(OpCode.LoadLocal, slot.Value, tok: nd.Tok);
                Expr(nd.B);
                if (op != AssignOp.Set) Emit(CompoundOp(op), tok: nd.Tok);
                Emit(OpCode.StoreLocal, slot.Value, tok: nd.Tok);
                break;
            }

            case NodeKind.Member:
            {
                if (target.Op == 1) { Error("?. 不能作为赋值目标", nd); return; }
                int site = SiteIdx(SiteKind.SetMember, _names[target.B], 0);
                Expr(target.A);
                if (op != AssignOp.Set)
                {
                    Emit(OpCode.Dup, tok: nd.Tok);
                    Emit(OpCode.GetMember, SiteIdx(SiteKind.GetMember, _names[target.B], 0), tok: nd.Tok);
                }
                Expr(nd.B);
                if (op != AssignOp.Set) Emit(CompoundOp(op), tok: nd.Tok);
                Emit(OpCode.SetMember, site, tok: nd.Tok);
                break;
            }

            case NodeKind.Index:
            {
                Expr(target.A);
                Expr(target.B);
                if (op != AssignOp.Set)
                {
                    Emit(OpCode.Dup2, tok: nd.Tok);
                    Emit(OpCode.GetIndex, tok: nd.Tok);
                }
                Expr(nd.B);
                if (op != AssignOp.Set) Emit(CompoundOp(op), tok: nd.Tok);
                Emit(OpCode.SetIndex, tok: nd.Tok);
                break;
            }

            default:
                Error("赋值目标只能是变量、成员或下标", nd);
                break;
        }
    }

    private static OpCode CompoundOp(AssignOp op) => op switch
    {
        AssignOp.AddSet => OpCode.Add,
        AssignOp.SubSet => OpCode.Sub,
        AssignOp.MulSet => OpCode.Mul,
        AssignOp.DivSet => OpCode.Div,
        _ => OpCode.Mod,
    };

    private void GuardNull(Node nd)
    {
        if (_safeChains.Count == 0) return; // 解析器保证 ?. 一定被 SafeChain 包住
        _safeChains.Peek().Add(Emit(OpCode.JumpIfNull, 0, tok: nd.Tok));
    }

    // ================================================================ 发射 / 作用域

    private int Emit(OpCode op, int a = 0, int b = 0, int tok = 0)
    {
        int ip = _code.Count;
        _code.Add(new Instr(op, a, b));

        var t = _ast.Tokens[Math.Clamp(tok, 0, _ast.Tokens.Length - 1)];
        _lines.Add(t.Line);
        _cols.Add(t.Col);

        _stack += StackEffect(op, a, b);
        if (_stack > _maxStack) _maxStack = _stack;
        if (_stack < 0) _stack = 0; // 分支合流处的估算误差，不影响正确性
        return ip;
    }

    private int StackEffect(OpCode op, int a, int b) => op switch
    {
        OpCode.PushConst or OpCode.PushInt or OpCode.PushNull or OpCode.PushType
            or OpCode.LoadLocal or OpCode.Dup => 1,
        OpCode.Dup2 => 2,
        OpCode.StoreLocal or OpCode.Pop or OpCode.IterInit => -1,
        OpCode.NewArray => 1 - a,
        OpCode.NewMap => 1 - b,
        OpCode.Add or OpCode.Sub or OpCode.Mul or OpCode.Div or OpCode.Mod
            or OpCode.Eq or OpCode.Ne or OpCode.Lt or OpCode.Le or OpCode.Gt or OpCode.Ge
            or OpCode.JumpIfFalse or OpCode.JumpIfTrue or OpCode.GetIndex or OpCode.Return => -1,
        OpCode.SetMember => -2,
        OpCode.SetIndex => -3,
        OpCode.Call => -_sites[a].ArgCount,
        OpCode.IterNext => 1,
        _ => 0,
    };

    private void Patch(int ip, int targetA)
    {
        var ins = _code[ip];
        _code[ip] = new Instr(ins.Op, targetA, ins.B);
    }

    private void PatchB(int ip, int targetB)
    {
        var ins = _code[ip];
        _code[ip] = new Instr(ins.Op, ins.A, targetB);
    }

    private int NewSlot(string name)
    {
        int s = _slotCount++;
        _visible.Add((name, s));
        return s;
    }

    private int? Find(string name)
    {
        for (int i = _visible.Count - 1; i >= 0; i--)
            if (string.Equals(_visible[i].Name, name, StringComparison.Ordinal)) return _visible[i].Slot;
        return _externalSlots.TryGetValue(name, out var s) ? s : null;
    }

    /// <summary>只在当前块作用域内查重，外层同名变量属于合法遮蔽。</summary>
    private bool DeclaredInCurrentScope(string name)
    {
        int from = _scopeMarks.Count > 0 ? _scopeMarks.Peek() : 0;
        for (int i = _visible.Count - 1; i >= from; i--)
            if (string.Equals(_visible[i].Name, name, StringComparison.Ordinal)) return true;
        return false;
    }

    private int EnterScope()
    {
        int mark = _visible.Count;
        _scopeMarks.Push(mark);
        return mark;
    }

    private void ExitScope(int mark)
    {
        _scopeMarks.Pop();
        _visible.RemoveRange(mark, _visible.Count - mark);
    }

    private int External(string name)
    {
        if (_externalSlots.TryGetValue(name, out var s)) return s;
        s = _slotCount++;
        _externalSlots[name] = s;
        _externalNames.Add(name);
        return s;
    }

    private int TypeIdx(Type t)
    {
        if (_typeIndex.TryGetValue(t, out var i)) return i;
        _types.Add(t);
        i = _types.Count - 1;
        _typeIndex[t] = i;
        return i;
    }

    private int SiteIdx(SiteKind kind, string name, int argc)
    {
        _sites.Add(new MemberSite(kind, name, argc));
        return _sites.Count - 1;
    }


    private void Error(string msg, Node nd)
    {
        var t = _ast.Tokens[Math.Clamp(nd.Tok, 0, _ast.Tokens.Length - 1)];
        _diags.Add(new Diagnostic(DiagSeverity.Error, msg, t.Line, t.Col));
    }
}

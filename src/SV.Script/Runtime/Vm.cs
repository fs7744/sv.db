using SV.Script.Emit;

namespace SV.Script.Runtime;

/// <summary>
/// 单帧字节码解释器。<see cref="Value"/> 含引用字段所以不能 stackalloc，改用线程本地缓冲，
/// 避免每次执行都去 ArrayPool 租借/归还（对短脚本那部分固定开销比脚本本身还大）。
/// </summary>
public static class Vm
{
    [ThreadStatic] private static Value[]? _tlsStack;
    [ThreadStatic] private static Value[]? _tlsArgs;

    /// <summary>宿主方法回调里又执行脚本时，线程本地缓冲已被占用，此时退化为临时数组。</summary>
    [ThreadStatic] private static bool _tlsBusy;

    public static Value Execute(ScriptProgram p, Value[] slots, long fuel)
    {
        if (slots.Length < p.SlotCount)
            throw new ArgumentException($"槽位数组太小：需要 {p.SlotCount}，实际 {slots.Length}", nameof(slots));

        bool reentrant = _tlsBusy;
        Value[] stackBuf, argBuf;

        if (reentrant)
        {
            stackBuf = new Value[p.MaxStack];
            argBuf = new Value[p.MaxArgs];
        }
        else
        {
            _tlsBusy = true;
            var cached = _tlsStack;
            stackBuf = cached is not null && cached.Length >= p.MaxStack
                ? cached
                : _tlsStack = new Value[Math.Max(64, p.MaxStack)];

            var cachedArgs = _tlsArgs;
            argBuf = cachedArgs is not null && cachedArgs.Length >= p.MaxArgs
                ? cachedArgs
                : _tlsArgs = new Value[Math.Max(8, p.MaxArgs)];
        }

        var st = stackBuf.AsSpan(0, p.MaxStack);
        var code = p.Code;
        int ip = 0;
        int sp = 0;

        try
        {
            while (true)
            {
                if (--fuel < 0) throw new ScriptFuelExhaustedException(p.Code.Length);

                var ins = code[ip++];
                switch (ins.Op)
                {
                    case OpCode.Nop:
                        break;

                    case OpCode.PushConst:
                        st[sp++] = p.Consts[ins.A];
                        break;

                    case OpCode.PushInt:
                        st[sp++] = Value.Int(ins.A);
                        break;

                    case OpCode.PushNull:
                        st[sp++] = Value.Null;
                        break;

                    case OpCode.PushType:
                        st[sp++] = Value.TypeRef(p.Types[ins.A]);
                        break;

                    case OpCode.LoadLocal:
                        st[sp++] = slots[ins.A];
                        break;

                    case OpCode.StoreLocal:
                        slots[ins.A] = st[--sp];
                        break;

                    case OpCode.Pop:
                        sp--;
                        break;

                    case OpCode.Dup:
                        st[sp] = st[sp - 1];
                        sp++;
                        break;

                    case OpCode.Dup2:
                        st[sp] = st[sp - 2];
                        st[sp + 1] = st[sp - 1];
                        sp += 2;
                        break;

                    case OpCode.NewArray:
                    {
                        int n = ins.A;
                        var a = new ScriptArray(n);
                        for (int i = sp - n; i < sp; i++) a.Add(st[i]);
                        sp -= n;
                        st[sp++] = Value.Arr(a);
                        break;
                    }

                    case OpCode.NewMap:
                    {
                        var keys = p.MapKeySets[ins.A];
                        int n = ins.B;
                        var m = new ScriptMap(n);
                        for (int i = 0; i < n; i++) m[keys[i]] = st[sp - n + i];
                        sp -= n;
                        st[sp++] = Value.Map(m);
                        break;
                    }

                    case OpCode.Add:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Ops.Add(st[sp - 1], b);
                        break;
                    }

                    case OpCode.Sub:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Ops.Sub(st[sp - 1], b);
                        break;
                    }

                    case OpCode.Mul:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Ops.Mul(st[sp - 1], b);
                        break;
                    }

                    case OpCode.Div:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Ops.Div(st[sp - 1], b);
                        break;
                    }

                    case OpCode.Mod:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Ops.Mod(st[sp - 1], b);
                        break;
                    }

                    case OpCode.Neg:
                        st[sp - 1] = Ops.Neg(st[sp - 1]);
                        break;

                    case OpCode.Not:
                        st[sp - 1] = Ops.Not(st[sp - 1]);
                        break;

                    case OpCode.AssertBool:
                        Ops.RequireBool(st[sp - 1], ins.A == 0 ? "and 的右侧" : "or 的右侧");
                        break;

                    case OpCode.Eq:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(st[sp - 1].Equals(b));
                        break;
                    }

                    case OpCode.Ne:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(!st[sp - 1].Equals(b));
                        break;
                    }

                    case OpCode.Lt:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(Ops.Compare(st[sp - 1], b) < 0);
                        break;
                    }

                    case OpCode.Le:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(Ops.Compare(st[sp - 1], b) <= 0);
                        break;
                    }

                    case OpCode.Gt:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(Ops.Compare(st[sp - 1], b) > 0);
                        break;
                    }

                    case OpCode.Ge:
                    {
                        var b = st[--sp];
                        st[sp - 1] = Value.Bool(Ops.Compare(st[sp - 1], b) >= 0);
                        break;
                    }

                    case OpCode.Jump:
                        ip = ins.A;
                        break;

                    case OpCode.JumpIfFalse:
                        if (!Ops.RequireBool(st[--sp], "条件")) ip = ins.A;
                        break;

                    case OpCode.JumpIfTrue:
                        if (Ops.RequireBool(st[--sp], "条件")) ip = ins.A;
                        break;

                    case OpCode.JumpIfFalseKeep:
                        if (!Ops.RequireBool(st[sp - 1], "and 的左侧")) ip = ins.A;
                        break;

                    case OpCode.JumpIfTrueKeep:
                        if (Ops.RequireBool(st[sp - 1], "or 的左侧")) ip = ins.A;
                        break;

                    case OpCode.JumpIfNull:
                        if (st[sp - 1].IsNull) ip = ins.A;
                        break;

                    case OpCode.JumpIfNotNull:
                        if (!st[sp - 1].IsNull) ip = ins.A;
                        break;

                    case OpCode.GetMember:
                        st[sp - 1] = Interop.GetMember(p.Sites[ins.A], st[sp - 1]);
                        break;

                    case OpCode.SetMember:
                    {
                        var v = st[--sp];
                        var recv = st[--sp];
                        Interop.SetMember(p.Sites[ins.A], recv, v);
                        break;
                    }

                    case OpCode.GetIndex:
                    {
                        var idx = st[--sp];
                        st[sp - 1] = Interop.GetIndex(st[sp - 1], idx);
                        break;
                    }

                    case OpCode.SetIndex:
                    {
                        var v = st[--sp];
                        var idx = st[--sp];
                        var recv = st[--sp];
                        Interop.SetIndex(recv, idx, v);
                        break;
                    }

                    case OpCode.Call:
                    {
                        var site = p.Sites[ins.A];
                        int argc = site.ArgCount;
                        sp -= argc;
                        for (int i = 0; i < argc; i++) argBuf[i] = st[sp + i];
                        var recv = st[--sp];
                        st[sp++] = Interop.Call(site, recv, argBuf);
                        break;
                    }

                    case OpCode.IterInit:
                    {
                        var src = st[--sp];
                        slots[ins.A] = Value.Obj(Marshaller.Enumerate(src));
                        break;
                    }

                    case OpCode.IterNext:
                    {
                        var en = (IEnumerator<Value>)slots[ins.A].O!;
                        if (en.MoveNext()) st[sp++] = en.Current;
                        else ip = ins.B;
                        break;
                    }

                    case OpCode.Return:
                        return st[--sp];

                    case OpCode.ReturnNull:
                        return Value.Null;

                    default:
                        throw new ScriptRuntimeException($"未实现的指令 {ins.Op}");
                }
            }
        }
        catch (ScriptRuntimeException ex) when (ex.Line == 0)
        {
            Locate(p, ip - 1, ex);
            throw;
        }
        catch (ScriptException)
        {
            throw;
        }
        catch (IndexOutOfRangeException ex)
        {
            var e = new ScriptRuntimeException("求值栈越界（编译器内部错误，请报告此脚本）", ex);
            Locate(p, ip - 1, e);
            throw e;
        }
        catch (Exception ex)
        {
            // 宿主方法抛出的异常包装成脚本错误，带上位置
            var e = new ScriptRuntimeException(ex.Message, ex);
            Locate(p, ip - 1, e);
            throw e;
        }
        finally
        {
            if (!reentrant)
            {
                // 只清用到的那一小段，避免线程本地缓冲长期吊住对象
                if (sp > 0) Array.Clear(stackBuf, 0, Math.Min(sp, stackBuf.Length));
                if (p.MaxArgs > 0) Array.Clear(argBuf, 0, p.MaxArgs);
                _tlsBusy = false;
            }
        }
    }

    private static void Locate(ScriptProgram p, int at, ScriptRuntimeException ex)
    {
        if (at < 0 || at >= p.Lines.Length) return;
        ex.Line = p.Lines[at];
        ex.Col = p.Cols[at];
        ex.SourceLine = SourceLine(p.Source, ex.Line);
    }

    private static string? SourceLine(string src, int line)
    {
        if (line <= 0) return null;
        int cur = 1, start = 0;
        for (int i = 0; i < src.Length; i++)
        {
            if (cur == line)
            {
                int end = src.IndexOf('\n', i);
                return end < 0 ? src[start..] : src[start..end].TrimEnd('\r');
            }
            if (src[i] == '\n') { cur++; start = i + 1; }
        }
        return cur == line ? src[start..] : null;
    }
}

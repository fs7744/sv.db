using System.Runtime.CompilerServices;

namespace SV.Script.Runtime;

/// <summary>
/// 算术与比较语义。整数走 long 快路径，溢出或出现小数时提升为 decimal。
/// 除法 <c>/</c> 始终产生 decimal，所以 <c>5 / 2</c> 是 2.5 而不是 2。
/// </summary>
public static class Ops
{
    public static Value Add(in Value a, in Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int)
        {
            long x = a.I, y = b.I;
            long r = unchecked(x + y);
            // 同号相加结果变号 => 溢出
            if (((x ^ r) & (y ^ r)) < 0) return Value.Dec((decimal)x + y);
            return Value.Int(r);
        }
        if (a.IsNumber && b.IsNumber) return Value.Dec(a.AsDec + b.AsDec);

        // 任一侧是字符串则做拼接，这是脚本里最自然的预期
        if (a.Kind == ValueKind.Str || b.Kind == ValueKind.Str)
            return Value.Str(a.ToDisplayString() + b.ToDisplayString());

        throw Bad("+", a, b);
    }

    public static Value Sub(in Value a, in Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int)
        {
            long x = a.I, y = b.I;
            long r = unchecked(x - y);
            if (((x ^ y) & (x ^ r)) < 0) return Value.Dec((decimal)x - y);
            return Value.Int(r);
        }
        if (a.IsNumber && b.IsNumber) return Value.Dec(a.AsDec - b.AsDec);
        throw Bad("-", a, b);
    }

    public static Value Mul(in Value a, in Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int)
        {
            long x = a.I, y = b.I;
            if (x == 0 || y == 0) return Value.Zero;
            long r = unchecked(x * y);
            // 反除还原不上则溢出；long.MinValue * -1 单独挡掉
            if (r / y != x || (y == -1 && x == long.MinValue) || (x == -1 && y == long.MinValue))
                return Value.Dec((decimal)x * y);
            return Value.Int(r);
        }
        if (a.IsNumber && b.IsNumber) return Value.Dec(a.AsDec * b.AsDec);
        throw Bad("*", a, b);
    }

    public static Value Div(in Value a, in Value b)
    {
        if (!a.IsNumber || !b.IsNumber) throw Bad("/", a, b);
        var d = b.AsDec;
        if (d == 0m) throw new ScriptRuntimeException("除数为 0");
        return Value.Dec(a.AsDec / d);
    }

    public static Value Mod(in Value a, in Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int)
        {
            if (b.I == 0) throw new ScriptRuntimeException("取模的除数为 0");
            if (b.I == -1 && a.I == long.MinValue) return Value.Zero;
            return Value.Int(a.I % b.I);
        }
        if (a.IsNumber && b.IsNumber)
        {
            var d = b.AsDec;
            if (d == 0m) throw new ScriptRuntimeException("取模的除数为 0");
            return Value.Dec(a.AsDec % d);
        }
        throw Bad("%", a, b);
    }

    public static Value Neg(in Value a)
    {
        if (a.Kind == ValueKind.Int)
            return a.I == long.MinValue ? Value.Dec(-(decimal)a.I) : Value.Int(-a.I);
        if (a.Kind == ValueKind.Dec) return Value.Dec(-a.D);
        throw new ScriptRuntimeException($"一元 - 不支持 {a.TypeName}");
    }

    public static Value Not(in Value a)
    {
        if (a.Kind != ValueKind.Bool)
            throw new ScriptRuntimeException($"! 需要 bool，得到 {a.TypeName}");
        return Value.Bool(!a.AsBool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(in Value a, in Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int) return a.I.CompareTo(b.I);
        if (a.IsNumber && b.IsNumber) return a.AsDec.CompareTo(b.AsDec);
        if (a.Kind == ValueKind.Str && b.Kind == ValueKind.Str)
            return string.CompareOrdinal(a.AsStr, b.AsStr);
        throw new ScriptRuntimeException($"无法比较 {a.TypeName} 和 {b.TypeName}");
    }

    /// <summary>条件位置要求严格 bool，避免 truthy 带来的隐式转换陷阱。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RequireBool(in Value v, string where)
    {
        if (v.Kind != ValueKind.Bool)
            throw new ScriptRuntimeException($"{where} 需要 bool，得到 {v.TypeName}");
        return v.AsBool;
    }

    /// <summary>把值转成数组下标。允许无小数部分的 decimal，于是 a[n / 2] 可用。</summary>
    public static int ToIndex(in Value v)
    {
        if (v.Kind == ValueKind.Int)
        {
            if (v.I < int.MinValue || v.I > int.MaxValue)
                throw new ScriptRuntimeException($"下标超出范围: {v.I}");
            return (int)v.I;
        }
        if (v.Kind == ValueKind.Dec)
        {
            if (decimal.Truncate(v.D) != v.D)
                throw new ScriptRuntimeException($"下标必须是整数，得到 {v.D}");
            if (v.D < int.MinValue || v.D > int.MaxValue)
                throw new ScriptRuntimeException($"下标超出范围: {v.D}");
            return (int)v.D;
        }
        throw new ScriptRuntimeException($"下标必须是整数，得到 {v.TypeName}");
    }

    private static ScriptRuntimeException Bad(string op, in Value a, in Value b)
        => new($"运算符 {op} 不支持 {a.TypeName} 和 {b.TypeName}");
}

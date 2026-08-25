using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SV.Script.Runtime;

/// <summary>
/// 脚本值与 CLR 值之间的转换。运行期助手是普通静态方法，
/// 同时提供表达式树构造器，让调用点在编译期就展开成直接调用，避免任何运行期反射。
/// </summary>
public static class Marshaller
{
    // ------------------------------------------------------------ CLR -> Value

    public static Value FromBool(bool v) => Value.Bool(v);

    public static Value FromI64(long v) => Value.Int(v);

    public static Value FromDec(decimal v) => Value.Dec(v);

    public static Value FromStr(string? v) => v is null ? Value.Null : Value.Str(v);

    public static Value FromChar(char v) => Value.Str(v.ToString());

    public static Value FromEnum(object? v) => v is null ? Value.Null : Value.Str(v.ToString()!);

    /// <summary>兜底转换：按运行期类型决定表示。</summary>
    public static Value FromClr(object? o) => o switch
    {
        null => Value.Null,
        Value v => v,
        bool b => Value.Bool(b),
        string s => Value.Str(s),
        ScriptArray a => Value.Arr(a),
        ScriptMap m => Value.Map(m),
        sbyte or byte or short or ushort or int or uint or long => Value.Int(Convert.ToInt64(o)),
        ulong u => u <= long.MaxValue ? Value.Int((long)u) : Value.Dec(u),
        decimal d => Value.Dec(d),
        float f => Value.Dec((decimal)f),
        double db => Value.Dec((decimal)db),
        char c => Value.Str(c.ToString()),
        Enum e => Value.Str(e.ToString()),
        Type t => Value.TypeRef(t),
        _ => Value.Obj(o),
    };

    // ------------------------------------------------------------ Value -> CLR

    public static bool ToBool(Value v) => v.Kind == ValueKind.Bool
        ? v.AsBool
        : throw Mismatch(v, "bool");

    public static string? ToStr(Value v) => v.Kind switch
    {
        ValueKind.Null => null,
        ValueKind.Str => v.AsStr,
        _ => v.ToDisplayString(),
    };

    public static char ToChar(Value v)
    {
        if (v.Kind == ValueKind.Str && v.AsStr.Length == 1) return v.AsStr[0];
        throw Mismatch(v, "char");
    }

    public static long ToI64(Value v) => v.Kind switch
    {
        ValueKind.Int => v.I,
        ValueKind.Dec => decimal.Truncate(v.D) == v.D && v.D >= long.MinValue && v.D <= long.MaxValue
            ? (long)v.D
            : throw Mismatch(v, "long"),
        _ => throw Mismatch(v, "long"),
    };

    public static int ToI32(Value v)
    {
        var l = ToI64(v);
        if (l < int.MinValue || l > int.MaxValue) throw Range(v, "int");
        return (int)l;
    }

    public static short ToI16(Value v)
    {
        var l = ToI64(v);
        if (l < short.MinValue || l > short.MaxValue) throw Range(v, "short");
        return (short)l;
    }

    public static sbyte ToI8(Value v)
    {
        var l = ToI64(v);
        if (l < sbyte.MinValue || l > sbyte.MaxValue) throw Range(v, "sbyte");
        return (sbyte)l;
    }

    public static byte ToU8(Value v)
    {
        var l = ToI64(v);
        if (l < byte.MinValue || l > byte.MaxValue) throw Range(v, "byte");
        return (byte)l;
    }

    public static ushort ToU16(Value v)
    {
        var l = ToI64(v);
        if (l < ushort.MinValue || l > ushort.MaxValue) throw Range(v, "ushort");
        return (ushort)l;
    }

    public static uint ToU32(Value v)
    {
        var l = ToI64(v);
        if (l < uint.MinValue || l > uint.MaxValue) throw Range(v, "uint");
        return (uint)l;
    }

    public static ulong ToU64(Value v)
    {
        var d = ToDec(v);
        if (d < 0m || d > ulong.MaxValue) throw Range(v, "ulong");
        return (ulong)d;
    }

    public static decimal ToDec(Value v) => v.IsNumber ? v.AsDec : throw Mismatch(v, "decimal");

    public static double ToF64(Value v) => v.IsNumber ? (double)v.AsDec : throw Mismatch(v, "double");

    public static float ToF32(Value v) => v.IsNumber ? (float)v.AsDec : throw Mismatch(v, "float");

    public static object ToEnum(Value v, Type t)
    {
        if (v.Kind == ValueKind.Str)
        {
            if (Enum.TryParse(t, v.AsStr, ignoreCase: true, out var r) && r is not null) return r;
            throw new ScriptRuntimeException($"'{v.AsStr}' 不是 {t.Name} 的有效值");
        }
        if (v.IsNumber) return Enum.ToObject(t, ToI64(v));
        throw Mismatch(v, t.Name);
    }

    /// <summary>兜底转换：目标是引用类型或未特化的值类型时使用。</summary>
    public static object? ToObj(Value v, Type t)
    {
        if (t == typeof(object)) return v.ToClrObject();

        if (v.IsNull)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) is null)
                throw new ScriptRuntimeException($"null 不能转换为 {t.Name}");
            return null;
        }

        var o = v.ToClrObject();
        if (o is not null && t.IsInstanceOfType(o)) return o;

        // 数组 -> IEnumerable / IList 等由 ScriptArray 自身满足；否则尝试逐元素转换
        if (v.Kind == ValueKind.Array && t.IsArray)
        {
            var elem = t.GetElementType()!;
            var src = v.AsArray;
            var arr = Array.CreateInstance(elem, src.Count);
            for (int i = 0; i < src.Count; i++) arr.SetValue(ToClr(src[i], elem), i);
            return arr;
        }

        if (o is IConvertible && typeof(IConvertible).IsAssignableFrom(t))
        {
            try { return Convert.ChangeType(o, t, System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception ex) { throw new ScriptRuntimeException($"{v.TypeName} 无法转换为 {t.Name}", ex); }
        }

        throw Mismatch(v, t.Name);
    }

    /// <summary>非表达式路径的通用转换，供兜底反射调用使用。</summary>
    public static object? ToClr(Value v, Type t)
    {
        if (t == typeof(Value)) return v;
        if (t == typeof(bool)) return ToBool(v);
        if (t == typeof(string)) return ToStr(v);
        if (t == typeof(long)) return ToI64(v);
        if (t == typeof(int)) return ToI32(v);
        if (t == typeof(short)) return ToI16(v);
        if (t == typeof(sbyte)) return ToI8(v);
        if (t == typeof(byte)) return ToU8(v);
        if (t == typeof(ushort)) return ToU16(v);
        if (t == typeof(uint)) return ToU32(v);
        if (t == typeof(ulong)) return ToU64(v);
        if (t == typeof(decimal)) return ToDec(v);
        if (t == typeof(double)) return ToF64(v);
        if (t == typeof(float)) return ToF32(v);
        if (t == typeof(char)) return ToChar(v);
        if (t.IsEnum) return ToEnum(v, t);
        var u = Nullable.GetUnderlyingType(t);
        if (u is not null) return v.IsNull ? null : ToClr(v, u);
        return ToObj(v, t);
    }

    // ------------------------------------------------------------ 表达式树构造

    private static MethodInfo M(string name) =>
        typeof(Marshaller).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    /// <summary>把一个产生 CLR 值的表达式包装成产生 <see cref="Value"/> 的表达式。</summary>
    public static Expression ToValueExpr(Expression e)
    {
        var t = e.Type;
        if (t == typeof(Value)) return e;
        if (t == typeof(bool)) return Expression.Call(M(nameof(FromBool)), e);
        if (t == typeof(string)) return Expression.Call(M(nameof(FromStr)), e);
        if (t == typeof(char)) return Expression.Call(M(nameof(FromChar)), e);
        if (t == typeof(decimal)) return Expression.Call(M(nameof(FromDec)), e);

        if (t == typeof(long) || t == typeof(int) || t == typeof(short) || t == typeof(sbyte)
            || t == typeof(byte) || t == typeof(ushort) || t == typeof(uint))
            return Expression.Call(M(nameof(FromI64)), Expression.Convert(e, typeof(long)));

        if (t == typeof(double) || t == typeof(float) || t == typeof(ulong))
            return Expression.Call(M(nameof(FromDec)), Expression.Convert(e, typeof(decimal)));

        if (t.IsEnum)
            return Expression.Call(M(nameof(FromEnum)), Expression.Convert(e, typeof(object)));

        // 可空值类型和其余一切走兜底
        return Expression.Call(M(nameof(FromClr)),
            t.IsValueType ? Expression.Convert(e, typeof(object)) : Expression.Convert(e, typeof(object)));
    }

    /// <summary>把一个产生 <see cref="Value"/> 的表达式转换成 <paramref name="target"/> 类型的表达式。</summary>
    public static Expression ToClrExpr(Expression valueExpr, Type target)
    {
        if (target == typeof(Value)) return valueExpr;
        if (target == typeof(bool)) return Expression.Call(M(nameof(ToBool)), valueExpr);
        if (target == typeof(string)) return Expression.Call(M(nameof(ToStr)), valueExpr);
        if (target == typeof(char)) return Expression.Call(M(nameof(ToChar)), valueExpr);
        if (target == typeof(long)) return Expression.Call(M(nameof(ToI64)), valueExpr);
        if (target == typeof(int)) return Expression.Call(M(nameof(ToI32)), valueExpr);
        if (target == typeof(short)) return Expression.Call(M(nameof(ToI16)), valueExpr);
        if (target == typeof(sbyte)) return Expression.Call(M(nameof(ToI8)), valueExpr);
        if (target == typeof(byte)) return Expression.Call(M(nameof(ToU8)), valueExpr);
        if (target == typeof(ushort)) return Expression.Call(M(nameof(ToU16)), valueExpr);
        if (target == typeof(uint)) return Expression.Call(M(nameof(ToU32)), valueExpr);
        if (target == typeof(ulong)) return Expression.Call(M(nameof(ToU64)), valueExpr);
        if (target == typeof(decimal)) return Expression.Call(M(nameof(ToDec)), valueExpr);
        if (target == typeof(double)) return Expression.Call(M(nameof(ToF64)), valueExpr);
        if (target == typeof(float)) return Expression.Call(M(nameof(ToF32)), valueExpr);

        if (target.IsEnum)
            return Expression.Convert(
                Expression.Call(M(nameof(ToEnum)), valueExpr, Expression.Constant(target, typeof(Type))),
                target);

        var u = Nullable.GetUnderlyingType(target);
        if (u is not null)
            return Expression.Condition(
                Expression.Property(valueExpr, nameof(Value.IsNull)),
                Expression.Default(target),
                Expression.Convert(ToClrExpr(valueExpr, u), target));

        return Expression.Convert(
            Expression.Call(M(nameof(ToObj)), valueExpr, Expression.Constant(target, typeof(Type))),
            target);
    }

    /// <summary>把宿主返回的可枚举对象包装成脚本可以 foreach 的枚举器。</summary>
    public static IEnumerator<Value> Enumerate(in Value v) => v.Kind switch
    {
        ValueKind.Array => ((IEnumerable<Value>)v.AsArray).GetEnumerator(),
        ValueKind.Map => MapKeys(v.AsMap),
        ValueKind.Str => Chars(v.AsStr),
        ValueKind.Null => throw new ScriptRuntimeException("不能对 null 做 foreach"),
        _ => v.O is IEnumerable e
            ? Wrap(e)
            : throw new ScriptRuntimeException($"{v.TypeName} 不可迭代（需要实现 IEnumerable）"),
    };

    private static IEnumerator<Value> MapKeys(ScriptMap m)
    {
        foreach (var kv in m) yield return Value.Str(kv.Key);
    }

    private static IEnumerator<Value> Chars(string s)
    {
        foreach (var c in s) yield return Value.Str(c.ToString());
    }

    private static IEnumerator<Value> Wrap(IEnumerable e)
    {
        foreach (var o in e) yield return FromClr(o);
    }

    private static ScriptRuntimeException Mismatch(Value v, string target)
        => new($"{v.TypeName} 无法转换为 {target}");

    private static ScriptRuntimeException Range(Value v, string target)
        => new($"{v.ToDisplayString()} 超出 {target} 的取值范围");
}

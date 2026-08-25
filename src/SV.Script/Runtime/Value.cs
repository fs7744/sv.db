using System.Globalization;
using System.Runtime.CompilerServices;

namespace SV.Script.Runtime;

public enum ValueKind : byte
{
    Null = 0,
    Bool,
    Int,
    Dec,
    Str,
    Array,
    Map,

    /// <summary>宿主对象（任意 CLR 实例）。</summary>
    Object,

    /// <summary>已注册的宿主类型，用于静态成员访问，如 Math.Round。</summary>
    Type,
}

/// <summary>
/// 脚本值。整数走 <see cref="long"/> 快路径，出现小数/除法时提升为 <see cref="decimal"/>。
/// 结构体按值传递，求值栈上不产生任何堆分配。
/// </summary>
public readonly struct Value : IEquatable<Value>
{
    public readonly ValueKind Kind;
    internal readonly long I;      // Int / Bool
    internal readonly decimal D;   // Dec
    internal readonly object? O;   // Str / Array / Map / Object / Type

    private Value(ValueKind kind, long i, decimal d, object? o)
    {
        Kind = kind; I = i; D = d; O = o;
    }

    public static readonly Value Null = default;
    public static readonly Value True = new(ValueKind.Bool, 1, 0m, null);
    public static readonly Value False = new(ValueKind.Bool, 0, 0m, null);
    public static readonly Value Zero = new(ValueKind.Int, 0, 0m, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Bool(bool b) => b ? True : False;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Int(long v) => new(ValueKind.Int, v, 0m, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Dec(decimal v) => new(ValueKind.Dec, 0, v, null);

    public static Value Str(string s) => new(ValueKind.Str, 0, 0m, s);

    public static Value Arr(ScriptArray a) => new(ValueKind.Array, 0, 0m, a);

    public static Value Map(ScriptMap m) => new(ValueKind.Map, 0, 0m, m);

    public static Value TypeRef(Type t) => new(ValueKind.Type, 0, 0m, t);

    /// <summary>
    /// 包装一个宿主对象，不做结构转换。传 null 得到 <see cref="Null"/>，
    /// 避免调用方拿到一个 Kind=Object 但内容为空的坏值。
    /// </summary>
    public static Value Obj(object? o) => o is null ? Null : new(ValueKind.Object, 0, 0m, o);

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == ValueKind.Null;
    }

    public bool IsNumber
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind is ValueKind.Int or ValueKind.Dec;
    }

    public bool AsBool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => I != 0;
    }

    public long AsInt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => I;
    }

    public decimal AsDec
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind == ValueKind.Int ? I : D;
    }

    public string AsStr => (string)O!;

    public ScriptArray AsArray => (ScriptArray)O!;

    public ScriptMap AsMap => (ScriptMap)O!;

    public Type AsType => (Type)O!;

    /// <summary>取出底层 CLR 对象；数字/布尔会装箱，仅用于宿主互操作边界。</summary>
    public object? ToClrObject() => Kind switch
    {
        ValueKind.Null => null,
        ValueKind.Bool => AsBool,
        ValueKind.Int => I,
        ValueKind.Dec => D,
        _ => O,
    };

    public string TypeName => Kind switch
    {
        ValueKind.Null => "null",
        ValueKind.Bool => "bool",
        ValueKind.Int => "int",
        ValueKind.Dec => "decimal",
        ValueKind.Str => "string",
        ValueKind.Array => "array",
        ValueKind.Map => "map",
        ValueKind.Type => "type " + ((Type)O!).Name,
        _ => O?.GetType().Name ?? "object",
    };

    public bool Equals(Value other)
    {
        // 数字跨表示比较：1 == 1.0 为真
        if (IsNumber && other.IsNumber)
        {
            if (Kind == ValueKind.Int && other.Kind == ValueKind.Int) return I == other.I;
            return AsDec == other.AsDec;
        }
        if (Kind != other.Kind) return false;
        return Kind switch
        {
            ValueKind.Null => true,
            ValueKind.Bool => I == other.I,
            ValueKind.Str => string.Equals((string)O!, (string)other.O!, StringComparison.Ordinal),
            _ => ReferenceEquals(O, other.O),
        };
    }

    public override bool Equals(object? obj) => obj is Value v && Equals(v);

    public override int GetHashCode() => Kind switch
    {
        ValueKind.Null => 0,
        ValueKind.Bool => I == 0 ? 1 : 2,
        ValueKind.Int => I.GetHashCode(),
        ValueKind.Dec => D == decimal.Truncate(D) && D >= long.MinValue && D <= long.MaxValue
            ? ((long)D).GetHashCode()
            : D.GetHashCode(),
        ValueKind.Str => ((string)O!).GetHashCode(StringComparison.Ordinal),
        _ => O?.GetHashCode() ?? 0,
    };

    /// <summary>脚本可见的字符串形式，用于字符串拼接和错误信息。</summary>
    public string ToDisplayString() => Kind switch
    {
        ValueKind.Null => "null",
        ValueKind.Bool => AsBool ? "true" : "false",
        ValueKind.Int => I.ToString(CultureInfo.InvariantCulture),
        ValueKind.Dec => D.ToString(CultureInfo.InvariantCulture),
        ValueKind.Str => (string)O!,
        ValueKind.Array => ((ScriptArray)O!).ToDisplayString(),
        ValueKind.Map => ((ScriptMap)O!).ToDisplayString(),
        ValueKind.Type => ((Type)O!).FullName ?? ((Type)O!).Name,
        _ => O?.ToString() ?? "null",
    };

    public override string ToString() => ToDisplayString();

    /// <summary>
    /// 结构精确比较：<see cref="Kind"/> 必须相同。常量池去重必须用它，
    /// 因为脚本语义下 <c>1 == 1.0</c> 为真，若按脚本语义去重就会把两个不同类型的字面量合并成一个。
    /// </summary>
    public static IEqualityComparer<Value> ExactComparer { get; } = new ExactEqualityComparer();

    private sealed class ExactEqualityComparer : IEqualityComparer<Value>
    {
        public bool Equals(Value a, Value b)
        {
            if (a.Kind != b.Kind) return false;
            return a.Kind switch
            {
                ValueKind.Null => true,
                ValueKind.Bool or ValueKind.Int => a.I == b.I,
                ValueKind.Dec => a.D == b.D && a.D.Scale == b.D.Scale,
                ValueKind.Str => string.Equals((string)a.O!, (string)b.O!, StringComparison.Ordinal),
                _ => ReferenceEquals(a.O, b.O),
            };
        }

        public int GetHashCode(Value v) => HashCode.Combine((int)v.Kind, v.Kind switch
        {
            ValueKind.Bool or ValueKind.Int => v.I.GetHashCode(),
            ValueKind.Dec => v.D.GetHashCode(),
            ValueKind.Str => ((string)v.O!).GetHashCode(StringComparison.Ordinal),
            ValueKind.Null => 0,
            _ => v.O?.GetHashCode() ?? 0,
        });
    }
}

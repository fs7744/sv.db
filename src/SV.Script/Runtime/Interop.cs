using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SV.Script.Runtime;

public delegate Value Invoker(object? recv, Value[] args);

public delegate void Setter(object? recv, Value value);

public enum SiteKind : byte { GetMember, SetMember, Call }

/// <summary>
/// 一个成员访问点或调用点。内联缓存以原子引用替换，竞争时最坏结果只是重复解析一次。
/// </summary>
public sealed class MemberSite
{
    public MemberSite(SiteKind kind, string name, int argCount)
    {
        Kind = kind; Name = name; ArgCount = argCount;
    }

    public SiteKind Kind { get; }

    public string Name { get; }

    public int ArgCount { get; }

    internal Entry? Cache;

    internal sealed class Entry
    {
        public Entry(Type target, int argSig, Invoker? invoke, Setter? set)
        {
            Target = target; ArgSig = argSig; Invoke = invoke; Set = set;
        }

        public readonly Type Target;

        /// <summary>实参 ValueKind 的折叠签名。不同实参类型可能选中不同重载，所以要一起做缓存键。</summary>
        public readonly int ArgSig;

        public readonly Invoker? Invoke;
        public readonly Setter? Set;
    }

    public override string ToString() => Kind == SiteKind.Call ? $"{Name}/{ArgCount}" : $"{Kind}:{Name}";
}

public static class Interop
{
    private static readonly Value[] NoArgs = [];

    private static readonly ConcurrentDictionary<(Type, bool), Invoker> IndexerCache = new();

    private static bool CanEmit => RuntimeFeature.IsDynamicCodeCompiled;

    // ---------------------------------------------------------------- 接收者归一化

    /// <summary>调用时传给 thunk 的接收者。数字/布尔在这里装箱，仅发生在宿主互操作边界。</summary>
    private static object? Receiver(in Value v) => v.Kind switch
    {
        ValueKind.Type => null,
        ValueKind.Bool or ValueKind.Int or ValueKind.Dec => v.ToClrObject(),
        _ => v.O,
    };

    private static Type TargetType(in Value v) => v.Kind switch
    {
        ValueKind.Type => v.AsType,
        ValueKind.Bool => typeof(bool),
        ValueKind.Int => typeof(long),
        ValueKind.Dec => typeof(decimal),
        ValueKind.Str => typeof(string),
        ValueKind.Array => typeof(ScriptArray),
        ValueKind.Map => typeof(ScriptMap),
        _ => v.O!.GetType(),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ArgSig(Value[] args, int argc)
    {
        int s = argc;
        int n = Math.Min(argc, 7);
        for (int i = 0; i < n; i++) s = (s << 4) | (int)args[i].Kind;
        return s;
    }

    // ---------------------------------------------------------------- 成员读

    public static Value GetMember(MemberSite site, in Value recv)
    {
        if (recv.IsNull)
            throw new ScriptRuntimeException($"不能在 null 上访问成员 '{site.Name}'");

        // map 上的成员访问优先当作键读取
        if (recv.Kind == ValueKind.Map)
        {
            var m = recv.AsMap;
            if (m.ContainsKey(site.Name)) return m[site.Name];
        }

        var target = TargetType(recv);
        var e = Volatile.Read(ref site.Cache);
        if (e is null || e.Target != target || e.Invoke is null)
        {
            e = new MemberSite.Entry(target, 0, BuildGetter(target, site.Name, recv.Kind == ValueKind.Type), null);
            Volatile.Write(ref site.Cache, e);
        }
        return e.Invoke!(Receiver(recv), NoArgs);
    }

    public static void SetMember(MemberSite site, in Value recv, in Value val)
    {
        if (recv.IsNull)
            throw new ScriptRuntimeException($"不能在 null 上写入成员 '{site.Name}'");

        if (recv.Kind == ValueKind.Map)
        {
            recv.AsMap[site.Name] = val;
            return;
        }

        var target = TargetType(recv);
        var e = Volatile.Read(ref site.Cache);
        if (e is null || e.Target != target || e.Set is null)
        {
            e = new MemberSite.Entry(target, 0, null, BuildSetter(target, site.Name, recv.Kind == ValueKind.Type));
            Volatile.Write(ref site.Cache, e);
        }
        e.Set!(Receiver(recv), val);
    }

    // ---------------------------------------------------------------- 方法调用

    public static Value Call(MemberSite site, in Value recv, Value[] args)
    {
        if (recv.IsNull)
            throw new ScriptRuntimeException($"不能在 null 上调用方法 '{site.Name}'");

        var target = TargetType(recv);
        int sig = ArgSig(args, site.ArgCount);

        var e = Volatile.Read(ref site.Cache);
        if (e is null || e.Target != target || e.ArgSig != sig || e.Invoke is null)
        {
            var inv = BuildInvoker(target, site.Name, args, site.ArgCount, recv.Kind == ValueKind.Type);
            e = new MemberSite.Entry(target, sig, inv, null);
            Volatile.Write(ref site.Cache, e);
        }
        return e.Invoke!(Receiver(recv), args);
    }

    // ---------------------------------------------------------------- 索引

    public static Value GetIndex(in Value recv, in Value index)
    {
        switch (recv.Kind)
        {
            case ValueKind.Null:
                throw new ScriptRuntimeException("不能对 null 取下标");
            case ValueKind.Array:
                return recv.AsArray[Ops.ToIndex(index)];
            case ValueKind.Map:
                if (index.Kind != ValueKind.Str)
                    throw new ScriptRuntimeException($"map 的键必须是字符串，得到 {index.TypeName}");
                return recv.AsMap[index.AsStr];
            case ValueKind.Str:
            {
                var s = recv.AsStr;
                int i = Ops.ToIndex(index);
                if ((uint)i >= (uint)s.Length)
                    throw new ScriptRuntimeException($"字符串下标越界: {i}，长度 {s.Length}");
                return Value.Str(s[i].ToString());
            }
        }

        var t = TargetType(recv);
        var inv = IndexerCache.GetOrAdd((t, false), key => BuildIndexer(key.Item1, false));
        return inv(Receiver(recv), [index]);
    }

    public static void SetIndex(in Value recv, in Value index, in Value val)
    {
        switch (recv.Kind)
        {
            case ValueKind.Null:
                throw new ScriptRuntimeException("不能对 null 写下标");
            case ValueKind.Array:
                recv.AsArray[Ops.ToIndex(index)] = val;
                return;
            case ValueKind.Map:
                if (index.Kind != ValueKind.Str)
                    throw new ScriptRuntimeException($"map 的键必须是字符串，得到 {index.TypeName}");
                recv.AsMap[index.AsStr] = val;
                return;
            case ValueKind.Str:
                throw new ScriptRuntimeException("字符串不可修改");
        }

        var t = TargetType(recv);
        var inv = IndexerCache.GetOrAdd((t, true), key => BuildIndexer(key.Item1, true));
        inv(Receiver(recv), [index, val]);
    }

    // ---------------------------------------------------------------- 解析：属性 / 字段

    private static Invoker BuildGetter(Type t, string name, bool isStatic)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        var pi = t.GetProperty(name, flags);
        if (pi is not null && pi.CanRead && pi.GetIndexParameters().Length == 0)
            return BuildMemberGetter(t, pi, pi.PropertyType, pi.GetMethod!.IsStatic);

        var fi = t.GetField(name, flags);
        if (fi is not null)
            return BuildMemberGetter(t, fi, fi.FieldType, fi.IsStatic);

        // ScriptMap 上未命中的成员读作缺失键，返回 null
        if (t == typeof(ScriptMap)) return static (_, _) => Value.Null;

        throw new ScriptRuntimeException($"{Describe(t, isStatic)} 上没有可读成员 '{name}'");
    }

    private static Invoker BuildMemberGetter(Type t, MemberInfo m, Type memberType, bool isStatic)
    {
        if (!CanEmit)
        {
            return m switch
            {
                PropertyInfo rp => (recv, _) => Marshaller.FromClr(rp.GetValue(recv)),
                _ => (recv, _) => Marshaller.FromClr(((FieldInfo)m).GetValue(recv)),
            };
        }

        var pRecv = Expression.Parameter(typeof(object), "recv");
        var pArgs = Expression.Parameter(typeof(Value[]), "args");
        Expression instance = isStatic ? null! : Expression.Convert(pRecv, t);
        Expression access = m is PropertyInfo p
            ? Expression.Property(instance, p)
            : Expression.Field(instance, (FieldInfo)m);
        return Expression.Lambda<Invoker>(Marshaller.ToValueExpr(access), pRecv, pArgs).Compile();
    }

    private static Setter BuildSetter(Type t, string name, bool isStatic)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        var pi = t.GetProperty(name, flags);
        if (pi is not null && pi.CanWrite && pi.GetIndexParameters().Length == 0)
        {
            if (!CanEmit)
            {
                var pt = pi.PropertyType;
                return (recv, v) => pi.SetValue(recv, Marshaller.ToClr(v, pt));
            }
            return BuildMemberSetter(t, pi, pi.PropertyType, pi.SetMethod!.IsStatic);
        }

        var fi = t.GetField(name, flags);
        if (fi is not null && !fi.IsInitOnly && !fi.IsLiteral)
        {
            if (!CanEmit)
            {
                var ft = fi.FieldType;
                return (recv, v) => fi.SetValue(recv, Marshaller.ToClr(v, ft));
            }
            return BuildMemberSetter(t, fi, fi.FieldType, fi.IsStatic);
        }

        throw new ScriptRuntimeException($"{Describe(t, isStatic)} 上没有可写成员 '{name}'");
    }

    private static Setter BuildMemberSetter(Type t, MemberInfo m, Type memberType, bool isStatic)
    {
        var pRecv = Expression.Parameter(typeof(object), "recv");
        var pVal = Expression.Parameter(typeof(Value), "value");
        Expression instance = isStatic ? null! : Expression.Convert(pRecv, t);
        Expression access = m is PropertyInfo p
            ? Expression.Property(instance, p)
            : Expression.Field(instance, (FieldInfo)m);
        var body = Expression.Assign(access, Marshaller.ToClrExpr(pVal, memberType));
        return Expression.Lambda<Setter>(body, pRecv, pVal).Compile();
    }

    // ---------------------------------------------------------------- 解析：方法重载

    private sealed class Binding
    {
        public required MethodInfo Method;
        public required ParameterInfo[] Params;
        public required int Score;

        /// <summary>非 null 表示最后一个形参是 params 数组，需要把多余实参打包。</summary>
        public Type? ParamsElem;
    }

    private static Invoker BuildInvoker(Type t, string name, Value[] args, int argc, bool isStatic)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        Binding? best = null;
        bool ambiguous = false;
        int nameHits = 0;

        foreach (var m in t.GetMethods(flags))
        {
            if (!string.Equals(m.Name, name, StringComparison.Ordinal)) continue;
            if (m.IsGenericMethodDefinition) continue; // 不做泛型推断
            nameHits++;

            var b = TryBind(m, args, argc);
            if (b is null) continue;

            if (best is null || b.Score < best.Score) { best = b; ambiguous = false; }
            else if (b.Score == best.Score) ambiguous = true;
        }

        if (best is null)
        {
            throw new ScriptRuntimeException(nameHits == 0
                ? $"{Describe(t, isStatic)} 上没有方法 '{name}'"
                : $"{Describe(t, isStatic)}.{name} 没有能接受 ({DescribeArgs(args, argc)}) 的重载");
        }
        if (ambiguous)
        {
            throw new ScriptRuntimeException(
                $"{Describe(t, isStatic)}.{name}({DescribeArgs(args, argc)}) 有多个同样匹配的重载，" +
                "请调整实参类型消除歧义");
        }

        return CanEmit ? EmitInvoker(t, best, argc) : ReflectInvoker(best, argc);
    }

    private static Binding? TryBind(MethodInfo m, Value[] args, int argc)
    {
        var ps = m.GetParameters();

        foreach (var p in ps)
        {
            if (p.ParameterType.IsByRef) return null; // 不支持 out/ref
        }

        // 情形 A：实参数量落在 [必需, 全部] 之间，缺的用默认值补
        if (argc <= ps.Length)
        {
            bool ok = true;
            int score = 0;
            for (int i = 0; i < argc; i++)
            {
                int s = ScoreParam(ps[i].ParameterType, args[i]);
                if (s < 0) { ok = false; break; }
                score += s;
            }
            if (ok)
            {
                for (int i = argc; i < ps.Length; i++)
                {
                    if (!ps[i].HasDefaultValue) { ok = false; break; }
                    score += 1; // 用默认值补参的轻微惩罚
                }
                if (ok) return new Binding { Method = m, Params = ps, Score = score };
            }
        }

        // 情形 B：最后一个形参是 params 数组
        if (ps.Length > 0 && argc >= ps.Length - 1)
        {
            var last = ps[^1];
            if (last.GetCustomAttribute<ParamArrayAttribute>() is not null && last.ParameterType.IsArray)
            {
                var elem = last.ParameterType.GetElementType()!;
                int score = 3; // params 展开的惩罚，让精确重载优先
                bool ok = true;
                for (int i = 0; i < ps.Length - 1; i++)
                {
                    int s = ScoreParam(ps[i].ParameterType, args[i]);
                    if (s < 0) { ok = false; break; }
                    score += s;
                }
                if (ok)
                {
                    for (int i = ps.Length - 1; i < argc; i++)
                    {
                        int s = ScoreParam(elem, args[i]);
                        if (s < 0) { ok = false; break; }
                        score += s;
                    }
                }
                if (ok) return new Binding { Method = m, Params = ps, Score = score, ParamsElem = elem };
            }
        }

        return null;
    }

    /// <summary>形参匹配打分。0 最好，-1 表示不兼容。</summary>
    private static int ScoreParam(Type target, in Value v)
    {
        if (target == typeof(Value)) return 0;

        switch (v.Kind)
        {
            case ValueKind.Null:
                if (target == typeof(string) || !target.IsValueType) return 1;
                return Nullable.GetUnderlyingType(target) is not null ? 1 : -1;

            case ValueKind.Bool:
                if (target == typeof(bool)) return 0;
                if (target == typeof(bool?)) return 1;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Int:
                if (target == typeof(long)) return 0;
                if (target == typeof(int)) return 1;
                if (target == typeof(decimal)) return 2;
                if (target == typeof(short) || target == typeof(byte) || target == typeof(sbyte)
                    || target == typeof(ushort) || target == typeof(uint) || target == typeof(ulong)) return 2;
                if (target == typeof(double) || target == typeof(float)) return 3;
                if (target.IsEnum) return 3;
                if (Nullable.GetUnderlyingType(target) is Type nu1) return ScoreParam(nu1, v) + 1;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Dec:
                if (target == typeof(decimal)) return 0;
                if (target == typeof(double)) return 1;
                if (target == typeof(float)) return 2;
                // 会截断，排在最后
                if (target == typeof(long) || target == typeof(int)) return 4;
                if (Nullable.GetUnderlyingType(target) is Type nu2) return ScoreParam(nu2, v) + 1;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Str:
                if (target == typeof(string)) return 0;
                if (target.IsEnum) return 2;
                if (target == typeof(char)) return 3;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Array:
                if (target == typeof(ScriptArray)) return 0;
                if (target.IsAssignableFrom(typeof(ScriptArray))) return 2;
                if (target.IsArray) return 4;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Map:
                if (target == typeof(ScriptMap)) return 0;
                if (target.IsAssignableFrom(typeof(ScriptMap))) return 2;
                return target == typeof(object) ? 5 : -1;

            case ValueKind.Type:
                return target == typeof(Type) ? 0 : target == typeof(object) ? 5 : -1;

            default:
            {
                var rt = v.O!.GetType();
                if (rt == target) return 0;
                if (target.IsAssignableFrom(rt)) return target.IsInterface ? 3 : 2;
                if (Nullable.GetUnderlyingType(target) is Type nu3) return ScoreParam(nu3, v) + 1;
                return target == typeof(object) ? 5 : -1;
            }
        }
    }

    private static Invoker EmitInvoker(Type t, Binding b, int argc)
    {
        var m = b.Method;
        var ps = b.Params;
        var pRecv = Expression.Parameter(typeof(object), "recv");
        var pArgs = Expression.Parameter(typeof(Value[]), "args");

        var argExprs = new Expression[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            if (b.ParamsElem is not null && i == ps.Length - 1)
            {
                var items = new Expression[Math.Max(0, argc - i)];
                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = Marshaller.ToClrExpr(
                        Expression.ArrayIndex(pArgs, Expression.Constant(i + j)), b.ParamsElem);
                }
                argExprs[i] = Expression.NewArrayInit(b.ParamsElem, items);
            }
            else if (i < argc)
            {
                argExprs[i] = Marshaller.ToClrExpr(
                    Expression.ArrayIndex(pArgs, Expression.Constant(i)), ps[i].ParameterType);
            }
            else
            {
                argExprs[i] = DefaultExpr(ps[i]);
            }
        }

        Expression call = m.IsStatic
            ? Expression.Call(m, argExprs)
            : Expression.Call(Expression.Convert(pRecv, t), m, argExprs);

        Expression body = m.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(Value.Null, typeof(Value)))
            : Marshaller.ToValueExpr(call);

        return Expression.Lambda<Invoker>(body, pRecv, pArgs).Compile();
    }

    private static Expression DefaultExpr(ParameterInfo p)
    {
        try
        {
            return Expression.Constant(p.DefaultValue, p.ParameterType);
        }
        catch (ArgumentException)
        {
            return Expression.Default(p.ParameterType);
        }
    }

    /// <summary>无法运行期生成代码（NativeAOT）时的回退路径。</summary>
    private static Invoker ReflectInvoker(Binding b, int argc)
    {
        var m = b.Method;
        var ps = b.Params;
        var elem = b.ParamsElem;
        bool isVoid = m.ReturnType == typeof(void);

        return (recv, args) =>
        {
            var call = new object?[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                if (elem is not null && i == ps.Length - 1)
                {
                    var extra = Array.CreateInstance(elem, Math.Max(0, argc - i));
                    for (int j = 0; j < extra.Length; j++) extra.SetValue(Marshaller.ToClr(args[i + j], elem), j);
                    call[i] = extra;
                }
                else if (i < argc) call[i] = Marshaller.ToClr(args[i], ps[i].ParameterType);
                else call[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
            }
            var r = m.Invoke(recv, call);
            return isVoid ? Value.Null : Marshaller.FromClr(r);
        };
    }

    // ---------------------------------------------------------------- 索引器

    private static Invoker BuildIndexer(Type t, bool write)
    {
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var ip = p.GetIndexParameters();
            if (ip.Length != 1) continue;
            var accessor = write ? p.SetMethod : p.GetMethod;
            if (accessor is null) continue;

            if (!CanEmit)
            {
                var keyType = ip[0].ParameterType;
                var valType = p.PropertyType;
                return write
                    ? (recv, args) =>
                    {
                        p.SetValue(recv, Marshaller.ToClr(args[1], valType), [Marshaller.ToClr(args[0], keyType)]);
                        return Value.Null;
                    }
                    : (recv, args) => Marshaller.FromClr(p.GetValue(recv, [Marshaller.ToClr(args[0], keyType)]));
            }

            var pRecv = Expression.Parameter(typeof(object), "recv");
            var pArgs = Expression.Parameter(typeof(Value[]), "args");
            var instance = Expression.Convert(pRecv, t);
            var key = Marshaller.ToClrExpr(Expression.ArrayIndex(pArgs, Expression.Constant(0)), ip[0].ParameterType);

            if (write)
            {
                var val = Marshaller.ToClrExpr(Expression.ArrayIndex(pArgs, Expression.Constant(1)), p.PropertyType);
                var body = Expression.Block(
                    Expression.Call(instance, accessor, key, val),
                    Expression.Constant(Value.Null, typeof(Value)));
                return Expression.Lambda<Invoker>(body, pRecv, pArgs).Compile();
            }

            var get = Marshaller.ToValueExpr(Expression.Call(instance, accessor, key));
            return Expression.Lambda<Invoker>(get, pRecv, pArgs).Compile();
        }

        var verb = write ? "写" : "读";
        return (_, _) => throw new ScriptRuntimeException($"{t.Name} 没有可{verb}的索引器");
    }

    // ---------------------------------------------------------------- 错误信息

    private static string Describe(Type t, bool isStatic) => isStatic ? $"类型 {t.Name}" : t.Name;

    private static string DescribeArgs(Value[] args, int argc)
    {
        if (argc == 0) return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < argc; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(args[i].TypeName);
        }
        return sb.ToString();
    }
}

using SV.Script.Runtime;

namespace SV.Script.Tests;

// ================================================================ 宿主测试类型

public enum OrderState { New = 0, Paid = 1, Shipped = 2 }

public class Entity
{
    public int Id { get; set; } = 7;

    public virtual string Kind => "entity";

    public string Trace() => "entity.Trace";
}

public interface IShippable
{
    string Region { get; }
}

public sealed class Address : IShippable
{
    public string Region { get; set; } = "CA";

    public string City { get; set; } = "Irvine";
}

public sealed class Customer
{
    public string Name { get; set; } = "";

    public bool IsVip { get; set; }

    public Customer? Referrer { get; set; }
}

public sealed class Order : Entity
{
    // ---- 属性 ----
    public string Code { get; set; } = "A-1";
    public decimal Total { get; set; }
    public int Count { get; set; }
    public double Weight { get; set; }
    public bool Rush { get; set; }
    public Customer? Customer { get; set; }
    public Address? Ship { get; set; }
    public IShippable? ShipAsInterface => Ship;
    public OrderState State { get; set; } = OrderState.New;
    public DateTime? PaidAt { get; set; }
    public int? Priority { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, int> Meta { get; } = new();
    public IReadOnlyList<int> Numbers { get; set; } = new[] { 1, 2, 3 };

    public override string Kind => "order";

    // ---- 字段 ----
    public int PlainField = 5;
    public readonly int ReadonlyField = 42;

    // ---- 方法 ----
    public decimal Discount(decimal rate) => Total * rate;

    public decimal Discount(decimal rate, int extra) => Total * rate - extra;

    public string Describe() => $"{Code}:{Total}";

    public string Join(string sep, params string[] parts) => string.Join(sep, parts);

    public int SumAll(params int[] ns) => ns.Sum();

    public string Label(string a, string b = "-", string c = "!") => a + b + c;

    public void Bump(int by = 1) => Count += by;

    public string? MaybeNull() => null;

    public void Boom() => throw new InvalidOperationException("host boom");

    public OrderState Next(OrderState s) => s == OrderState.Shipped ? OrderState.Shipped : (OrderState)((int)s + 1);

    /// <summary>两个重载对整数实参得分相同，用于验证歧义会被明确报错而不是乱猜。</summary>
    public string Ambiguous(short a) => "short";

    public string Ambiguous(byte a) => "byte";

    /// <summary>泛型方法定义应被跳过，不参与解析。</summary>
    public T Echo<T>(T v) => v;
}

public sealed class Matrix
{
    private readonly Dictionary<int, string> _cells = new() { [0] = "a", [1] = "b" };

    public string this[int i]
    {
        get => _cells.TryGetValue(i, out var v) ? v : "?";
        set => _cells[i] = value;
    }

    public int Count => _cells.Count;
}

public sealed class NoIndexer
{
    public int X { get; set; } = 1;
}

public static class Tax
{
    public const decimal Rate = 0.08m;

    public static string Region = "US";

    public static readonly string Fixed = "F";

    public static decimal For(string region) => region == "CA" ? 0.0925m : Rate;

    public static decimal Apply(decimal amount, decimal rate) => amount * (1 + rate);
}

/// <summary>宿主方法里再执行脚本，用于验证 VM 的线程本地缓冲可重入。</summary>
public sealed class Reentrant
{
    public long Double(long n)
    {
        var s = TestEngine.Shared.GetOrCompile("return v * 2;");
        var slots = s.CreateSlots();
        slots[s.SlotOf("v")] = Value.Int(n);
        return s.Run(slots).AsInt;
    }
}

// ================================================================ 测试基础设施

public static class TestEngine
{
    /// <summary>供 <see cref="Reentrant"/> 用的共享引擎，测试之间只读复用。</summary>
    public static readonly ScriptEngine Shared = New();

    public static ScriptEngine New(ScriptOptions? options = null)
        => new ScriptEngine(options)
            .RegisterType(typeof(Math))
            .RegisterType(typeof(Tax))
            .RegisterType(typeof(OrderState), "OrderState")
            .RegisterType(typeof(string), "Str")
            .RegisterType(typeof(Guid));

    public static Order SampleOrder() => new()
    {
        Code = "X-9",
        Total = 200m,
        Count = 2,
        Weight = 1.5,
        Customer = new Customer { Name = "Ann", IsVip = true },
        Ship = new Address { Region = "CA", City = "Irvine" },
        Tags = { "red", "blue" },
        State = OrderState.Paid,
        Numbers = new[] { 4, 5, 6 },
    };

    /// <summary>无外部变量的求值，返回显示串。</summary>
    public static string Eval(string src) => Run(src, null).ToDisplayString();

    /// <summary>注入名为 order 的变量后求值。</summary>
    public static string EvalWith(string src, object? order)
        => Run(src, new Dictionary<string, object?> { ["order"] = order }).ToDisplayString();

    public static Value Run(string src, IReadOnlyDictionary<string, object?>? vars)
        => New().Compile(src).Run(vars);

    public static Value RunVars(string src, params (string Name, object? Value)[] vars)
        => New().Compile(src).Run(vars.ToDictionary(v => v.Name, v => v.Value));

    /// <summary>断言脚本在运行期抛错，返回异常以便进一步检查。</summary>
    public static ScriptRuntimeException Fails(string src, object? order = null)
        => Assert.Throws<ScriptRuntimeException>(() => EvalWith(src, order));

    /// <summary>断言脚本编译不通过，返回全部诊断。</summary>
    public static IReadOnlyList<Syntax.Diagnostic> FailsToCompile(string src)
    {
        var ok = New().TryCompile(src, out _, out var diags);
        Assert.False(ok, "本以为编译会失败，实际却成功了");
        Assert.NotEmpty(diags);
        return diags;
    }
}

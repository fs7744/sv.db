using System.Collections.Concurrent;
using SV.Script.Emit;
using SV.Script.Runtime;
using SV.Script.Syntax;

namespace SV.Script;

public sealed class ScriptOptions
{
    /// <summary>true 时未声明的标识符是编译错误；false 时自动成为外部注入变量。</summary>
    public bool StrictVariables { get; set; }

    /// <summary>单次执行的指令预算，防死循环。</summary>
    public long Fuel { get; set; } = 20_000_000;
}

/// <summary>
/// 脚本引擎。注册宿主类型后编译脚本，编译产物可缓存、可跨线程并发执行。
/// </summary>
public sealed class ScriptEngine
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, CompiledScript> _cache = new(StringComparer.Ordinal);

    public ScriptEngine(ScriptOptions? options = null) => Options = options ?? new ScriptOptions();

    public ScriptOptions Options { get; }

    /// <summary>注册一个宿主类型，脚本里即可用 <c>别名.静态成员</c> 访问，例如 <c>Math.Round(x, 2)</c>。</summary>
    public ScriptEngine RegisterType(Type type, string? alias = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        _types[alias ?? type.Name] = type;
        _cache.Clear();
        return this;
    }

    public ScriptEngine RegisterType<T>(string? alias = null) => RegisterType(typeof(T), alias);

    /// <summary>编译脚本。有错误则抛 <see cref="ScriptCompileException"/>。</summary>
    public CompiledScript Compile(string source)
    {
        if (!TryCompile(source, out var script, out var diagnostics))
            throw new ScriptCompileException(diagnostics);
        return script!;
    }

    public bool TryCompile(string source, out CompiledScript? script, out IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);

        var diags = new List<Diagnostic>();
        var lexer = new Lexer(source, diags);
        var tokens = lexer.Tokenize();
        var ast = new Parser(source, tokens, lexer.StringPool, diags).Parse();
        var compiler = new Compiler(ast, _types, diags, Options.StrictVariables);
        var program = compiler.Compile();

        diagnostics = diags;
        if (diags.Any(d => d.Severity == DiagSeverity.Error))
        {
            script = null;
            return false;
        }

        script = new CompiledScript(program, compiler.ExternalSlots, Options.Fuel);
        return true;
    }

    /// <summary>按源码文本缓存编译结果。同一段脚本只编译一次。</summary>
    public CompiledScript GetOrCompile(string source) => _cache.GetOrAdd(source, Compile);

    /// <summary>一次性求值，图方便用；反复执行请用 <see cref="Compile"/> 后复用。</summary>
    public Value Evaluate(string source, IReadOnlyDictionary<string, object?>? variables = null)
        => GetOrCompile(source).Run(variables);
}

/// <summary>编译好的脚本。不可变，可跨线程并发 <see cref="Run(System.Collections.Generic.IReadOnlyDictionary{string,object})"/>。</summary>
public sealed class CompiledScript
{
    private readonly IReadOnlyDictionary<string, int> _externalSlots;

    internal CompiledScript(ScriptProgram program, IReadOnlyDictionary<string, int> externalSlots, long fuel)
    {
        Program = program;
        _externalSlots = externalSlots;
        Fuel = fuel;
    }

    public ScriptProgram Program { get; }

    public long Fuel { get; set; }

    /// <summary>脚本引用到的外部变量名。宿主可以据此校验自己提供的上下文是否完整。</summary>
    public IReadOnlyList<string> Externals => Program.ExternalNames;

    /// <summary>外部变量对应的槽位号，-1 表示脚本没有引用它。</summary>
    public int SlotOf(string name) => _externalSlots.TryGetValue(name, out var s) ? s : -1;

    public Value[] CreateSlots() => new Value[Program.SlotCount];

    public Value Run(IReadOnlyDictionary<string, object?>? variables = null)
    {
        var slots = CreateSlots();
        if (variables is not null)
        {
            foreach (var kv in variables)
            {
                int s = SlotOf(kv.Key);
                if (s >= 0) slots[s] = Marshaller.FromClr(kv.Value);
            }
        }
        return Vm.Execute(Program, slots, Fuel);
    }

    /// <summary>
    /// 高频路径：宿主先用 <see cref="SlotOf"/> 拿到槽位号，再复用 slots 数组反复执行，
    /// 全程没有字典查找。slots 每次执行前需要重置或重填。
    /// </summary>
    public Value Run(Value[] slots) => Vm.Execute(Program, slots, Fuel);

    public string Disassemble() => Program.Disassemble();
}

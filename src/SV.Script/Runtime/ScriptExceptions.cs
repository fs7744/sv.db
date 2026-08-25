using SV.Script.Syntax;

namespace SV.Script.Runtime;

public abstract class ScriptException : Exception
{
    protected ScriptException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>编译期错误。<see cref="Diagnostics"/> 保留全部诊断，不只是第一条。</summary>
public sealed class ScriptCompileException : ScriptException
{
    public ScriptCompileException(IReadOnlyList<Diagnostic> diagnostics)
        : base(Format(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    private static string Format(IReadOnlyList<Diagnostic> d)
    {
        if (d.Count == 0) return "脚本编译失败";
        var head = string.Join(Environment.NewLine, d.Take(10));
        return d.Count > 10
            ? head + Environment.NewLine + $"... 另有 {d.Count - 10} 条诊断"
            : head;
    }
}

/// <summary>运行期错误。位置由 VM 在捕获时填入。</summary>
public sealed class ScriptRuntimeException : ScriptException
{
    public ScriptRuntimeException(string message, Exception? inner = null) : base(message, inner) { }

    public int Line { get; internal set; }

    public int Col { get; internal set; }

    /// <summary>发生错误时的脚本源码行，便于定位。</summary>
    public string? SourceLine { get; internal set; }

    public override string ToString()
    {
        var head = Line > 0 ? $"({Line},{Col}): {Message}" : Message;
        if (SourceLine is not null) head += Environment.NewLine + "    " + SourceLine.Trim();
        if (InnerException is not null) head += Environment.NewLine + "---> " + InnerException;
        return head;
    }
}

/// <summary>指令预算耗尽（防死循环）。</summary>
public sealed class ScriptFuelExhaustedException : ScriptException
{
    public ScriptFuelExhaustedException(long fuel)
        : base($"脚本执行超出指令预算 {fuel}，可能存在死循环") { }
}

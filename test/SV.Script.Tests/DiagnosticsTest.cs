using SV.Script.Runtime;
using SV.Script.Syntax;

namespace SV.Script.Tests;

public class DiagnosticsTest
{
    private static IReadOnlyList<Diagnostic> Bad(string src) => TestEngine.FailsToCompile(src);

    // ---------------------------------------------------------------- 语法错误

    [Theory]
    [InlineData("return 1")]                        // 缺分号
    [InlineData("let a = 1")]
    [InlineData("let = 1;")]                        // 缺变量名
    [InlineData("let a 1;")]                        // 缺等号导致缺分号
    [InlineData("if true { return 1; }")]           // 缺括号
    [InlineData("if (true { return 1; }")]          // 缺右括号
    [InlineData("if (true) { return 1;")]           // 缺右大括号
    [InlineData("while { }")]
    [InlineData("for (let i = 0 i < 3; i += 1) { }")]
    [InlineData("foreach (v [1]) { }")]             // 缺 in
    [InlineData("foreach (v in [1]) ")]             // 缺循环体
    [InlineData("return 1 +;")]                     // 缺右操作数
    [InlineData("return (1;")]
    [InlineData("return [1;")]
    [InlineData("return { a: ;")]
    [InlineData("return true ? 1;")]                // 缺冒号
    [InlineData("return .5;")]                      // 不支持省略整数部分
    [InlineData("break")]
    [InlineData("a.;")]
    public void SyntaxErrorsAreReported(string src) => Assert.NotEmpty(Bad(src));

    [Fact]
    public void MissingBraceInElseIfChainIsReported()
    {
        // 用户最初那段脚本里第二个分支少了一个 }
        const string src = """
            if (x > 1) {
                return 1;
            } else if (x > 6) {
                return 2;
            else {
                return 3;
            }
            """;
        Assert.NotEmpty(Bad(src));
    }

    [Fact]
    public void DiagnosticCarriesAccuratePosition()
    {
        var d = Bad("let a = 1;\nlet b = 2\nlet c = 3;").First();
        // "let" 出现在需要分号的位置，报在第 3 行
        Assert.Equal(3, d.Line);
        Assert.Equal(1, d.Col);
    }

    [Fact]
    public void DiagnosticMessageNamesTheExpectedToken()
    {
        var d = Bad("if (true { return 1; }").First();
        Assert.Contains("')'", d.Message);
    }

    [Fact]
    public void DiagnosticMessageNamesTheFoundToken()
        => Assert.Contains("文件结束", Bad("return 1 +").First().Message);

    [Fact]
    public void MultipleErrorsAreAllCollected()
    {
        var d = Bad("let = 1;\nlet = 2;\nlet = 3;");
        Assert.True(d.Count >= 3, $"本以为至少 3 条诊断，实际 {d.Count}");
    }

    [Fact]
    public void ParserRecoversAndDoesNotHang()
    {
        // 错误恢复必须保证 token 指针前进，否则会死循环
        Assert.NotEmpty(Bad(") ) ) ] ] } }"));
        Assert.NotEmpty(Bad(new string(')', 200)));
    }

    [Fact]
    public void CompileExceptionMessageTruncatesLongDiagnosticLists()
    {
        var src = string.Join("\n", Enumerable.Range(0, 15).Select(_ => "@"));
        var ex = Assert.Throws<ScriptCompileException>(() => TestEngine.New().Compile(src));
        Assert.True(ex.Diagnostics.Count >= 15);
        Assert.Contains("另有", ex.Message);
    }

    [Fact]
    public void CompileExceptionExposesAllDiagnostics()
    {
        var ex = Assert.Throws<ScriptCompileException>(() => TestEngine.New().Compile("let = 1;\nlet = 2;"));
        Assert.All(ex.Diagnostics, d => Assert.Equal(DiagSeverity.Error, d.Severity));
        Assert.True(ex.Diagnostics.Count >= 2);
    }

    [Fact]
    public void DiagnosticToStringIsReadable()
    {
        var d = Bad("if (true { return 1; }").First();
        var text = d.ToString();
        Assert.Contains("error", text);
        Assert.Contains($"({d.Line},{d.Col})", text);
    }

    // ---------------------------------------------------------------- 语义（编译期）错误

    [Fact]
    public void BareFunctionCallIsReported()
        => Assert.Contains(Bad("return foo(1);"), d => d.Message.Contains("foo"));

    [Fact]
    public void BareFunctionCallHintsAtHostTypes()
        => Assert.Contains(Bad("return foo(1);"), d => d.Message.Contains("宿主类型"));

    [Fact]
    public void CallingATypeIsReported()
        => Assert.Contains(Bad("return Math(1);"), d => d.Message.Contains("是类型"));

    [Fact]
    public void AssigningToATypeIsReported()
        => Assert.Contains(Bad("Math = 1;"), d => d.Message.Contains("类型"));

    [Fact]
    public void CallingANonMemberIsReported()
        => Assert.Contains(Bad("return 1();"), d => d.Message.Contains("只能调用成员方法"));

    [Fact]
    public void InvalidAssignmentTargetIsReported()
    {
        Assert.Contains(Bad("1 = 2;"), d => d.Message.Contains("赋值目标"));
        Assert.Contains(Bad("\"a\" += 1;"), d => d.Message.Contains("赋值目标"));
    }

    [Fact]
    public void RedeclarationIsReported()
        => Assert.Contains(Bad("let a = 1; let a = 2;"), d => d.Message.Contains("已声明"));

    [Fact]
    public void BreakAndContinueOutsideLoopAreReported()
    {
        Assert.Contains(Bad("break;"), d => d.Message.Contains("break"));
        Assert.Contains(Bad("continue;"), d => d.Message.Contains("continue"));
        Assert.Contains(Bad("if (true) { break; }"), d => d.Message.Contains("break"));
    }

    // ---------------------------------------------------------------- 严格变量模式

    private static ScriptEngine Strict() => TestEngine.New(new ScriptOptions { StrictVariables = true });

    [Fact]
    public void StrictModeRejectsUndeclaredRead()
    {
        Assert.False(Strict().TryCompile("return zzz + 1;", out _, out var d));
        Assert.Contains(d, x => x.Message.Contains("zzz"));
    }

    [Fact]
    public void StrictModeRejectsUndeclaredWrite()
    {
        Assert.False(Strict().TryCompile("zzz = 1;", out _, out var d));
        Assert.Contains(d, x => x.Message.Contains("let"));
    }

    [Fact]
    public void StrictModeStillAllowsDeclaredVariables()
    {
        Assert.True(Strict().TryCompile("let a = 1; return a;", out var s, out _));
        Assert.Equal("1", s!.Run().ToDisplayString());
    }

    [Fact]
    public void StrictModeStillAllowsRegisteredTypes()
    {
        Assert.True(Strict().TryCompile("return Tax.Rate;", out var s, out _));
        Assert.Equal("0.08", s!.Run().ToDisplayString());
    }

    [Fact]
    public void NonStrictModeTurnsUnknownNamesIntoExternals()
    {
        var script = TestEngine.New().Compile("return zzz;");
        Assert.Equal(["zzz"], script.Externals);
        Assert.Equal("null", script.Run().ToDisplayString());
    }

    // ---------------------------------------------------------------- 运行期错误信息

    [Fact]
    public void RuntimeErrorCarriesLineColumnAndSourceLine()
    {
        var script = TestEngine.New().Compile("let a = 1;\nlet b = 0;\nreturn a / b;\n");
        var ex = Assert.Throws<ScriptRuntimeException>(() => script.Run());

        Assert.Equal(3, ex.Line);
        Assert.True(ex.Col > 0);
        Assert.Contains("除数为 0", ex.Message);
        Assert.Contains("return a / b;", ex.SourceLine);
    }

    [Fact]
    public void RuntimeErrorPositionPointsAtTheFailingLineInALoop()
    {
        var script = TestEngine.New().Compile("""
            let s = 0;
            for (let i = 0; i < 3; i += 1) {
                s += 1;
                s = s / 0;
            }
            return s;
            """);
        var ex = Assert.Throws<ScriptRuntimeException>(() => script.Run());
        Assert.Equal(4, ex.Line);
    }

    [Fact]
    public void RuntimeErrorToStringIncludesPositionAndSource()
    {
        var script = TestEngine.New().Compile("return 1 / 0;");
        var ex = Assert.Throws<ScriptRuntimeException>(() => script.Run());
        var text = ex.ToString();
        Assert.Contains("(1,", text);
        Assert.Contains("return 1 / 0;", text);
    }

    [Fact]
    public void RuntimeErrorOnFirstLineWorks()
    {
        var ex = Assert.Throws<ScriptRuntimeException>(() => TestEngine.New().Compile("return 1 % 0;").Run());
        Assert.Equal(1, ex.Line);
    }

    [Fact]
    public void RuntimeErrorOnLastLineWithoutTrailingNewlineWorks()
    {
        var ex = Assert.Throws<ScriptRuntimeException>(
            () => TestEngine.New().Compile("let a = 1;\nreturn a / 0;").Run());
        Assert.Equal(2, ex.Line);
        Assert.Contains("return a / 0;", ex.SourceLine);
    }

    [Fact]
    public void HostExceptionKeepsInnerException()
    {
        var script = TestEngine.New().Compile("order.Boom();");
        var ex = Assert.Throws<ScriptRuntimeException>(
            () => script.Run(new Dictionary<string, object?> { ["order"] = TestEngine.SampleOrder() }));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("host boom", ex.ToString());
    }

    // ---------------------------------------------------------------- 指令预算

    [Fact]
    public void InfiniteWhileIsStopped()
    {
        var script = TestEngine.New(new ScriptOptions { Fuel = 50_000 }).Compile("while (true) { }");
        Assert.Throws<ScriptFuelExhaustedException>(() => script.Run());
    }

    [Fact]
    public void InfiniteForIsStopped()
    {
        var script = TestEngine.New(new ScriptOptions { Fuel = 50_000 }).Compile("for (;;) { }");
        Assert.Throws<ScriptFuelExhaustedException>(() => script.Run());
    }

    [Fact]
    public void NonTerminatingLoopWithBodyIsStopped()
    {
        var script = TestEngine.New(new ScriptOptions { Fuel = 100_000 })
            .Compile("let a = []; while (true) { a.Add(1); }");
        Assert.Throws<ScriptFuelExhaustedException>(() => script.Run());
    }

    [Fact]
    public void FuelIsPerExecutionNotCumulative()
    {
        var script = TestEngine.New(new ScriptOptions { Fuel = 10_000 })
            .Compile("let s = 0; for (let i = 0; i < 100; i += 1) { s += i; } return s;");

        // 每次执行都是新的预算，跑很多次也不会耗尽
        for (int i = 0; i < 50; i++) Assert.Equal("4950", script.Run().ToDisplayString());
    }

    [Fact]
    public void FuelCanBeOverriddenPerScript()
    {
        var script = TestEngine.New().Compile("let s = 0; for (let i = 0; i < 1000; i += 1) { s += i; } return s;");
        Assert.Equal("499500", script.Run().ToDisplayString());

        script.Fuel = 100;
        Assert.Throws<ScriptFuelExhaustedException>(() => script.Run());
    }

    [Fact]
    public void FuelMessageMentionsDeadLoop()
    {
        var script = TestEngine.New(new ScriptOptions { Fuel = 1_000 }).Compile("while (true) { }");
        var ex = Assert.Throws<ScriptFuelExhaustedException>(() => script.Run());
        Assert.Contains("死循环", ex.Message);
    }

    // ---------------------------------------------------------------- 异常类型层次

    [Fact]
    public void AllScriptErrorsShareABaseType()
    {
        Assert.IsAssignableFrom<ScriptException>(
            Assert.Throws<ScriptCompileException>(() => TestEngine.New().Compile("let = 1;")));

        Assert.IsAssignableFrom<ScriptException>(
            Assert.Throws<ScriptRuntimeException>(() => TestEngine.New().Compile("return 1 / 0;").Run()));

        Assert.IsAssignableFrom<ScriptException>(Assert.Throws<ScriptFuelExhaustedException>(
            () => TestEngine.New(new ScriptOptions { Fuel = 10 }).Compile("while (true) { }").Run()));
    }
}

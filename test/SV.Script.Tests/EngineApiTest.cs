using SV.Script.Runtime;

namespace SV.Script.Tests;

public class EngineApiTest
{
    // ---------------------------------------------------------------- 编译入口

    [Fact]
    public void CompileThrowsOnError()
    {
        var ex = Assert.Throws<ScriptCompileException>(() => TestEngine.New().Compile("let = 1;"));
        Assert.NotEmpty(ex.Diagnostics);
    }

    [Fact]
    public void TryCompileReturnsFalseAndDiagnosticsOnError()
    {
        Assert.False(TestEngine.New().TryCompile("let = 1;", out var script, out var diags));
        Assert.Null(script);
        Assert.NotEmpty(diags);
    }

    [Fact]
    public void TryCompileReturnsTrueAndScriptOnSuccess()
    {
        Assert.True(TestEngine.New().TryCompile("return 1;", out var script, out var diags));
        Assert.NotNull(script);
        Assert.Empty(diags);
        Assert.Equal("1", script!.Run().ToDisplayString());
    }

    [Fact]
    public void NullSourceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TestEngine.New().Compile(null!));
        Assert.Throws<ArgumentNullException>(() => TestEngine.New().TryCompile(null!, out _, out _));
    }

    [Fact]
    public void NullTypeIsRejected()
        => Assert.Throws<ArgumentNullException>(() => TestEngine.New().RegisterType(null!));

    // ---------------------------------------------------------------- 缓存

    [Fact]
    public void GetOrCompileReturnsTheSameInstance()
    {
        var engine = TestEngine.New();
        var a = engine.GetOrCompile("return 1;");
        var b = engine.GetOrCompile("return 1;");
        Assert.Same(a, b);
    }

    [Fact]
    public void GetOrCompileDistinguishesDifferentSources()
    {
        var engine = TestEngine.New();
        Assert.NotSame(engine.GetOrCompile("return 1;"), engine.GetOrCompile("return 2;"));
    }

    [Fact]
    public void RegisteringATypeInvalidatesTheCache()
    {
        var engine = new ScriptEngine();
        var before = engine.GetOrCompile("return 1;");
        engine.RegisterType(typeof(Tax));
        Assert.NotSame(before, engine.GetOrCompile("return 1;"));
    }

    [Fact]
    public void TypesRegisteredAfterCompileDoNotAffectExistingScripts()
    {
        var engine = new ScriptEngine();
        var script = engine.Compile("return Tax;");       // Tax 此刻只是个外部变量
        engine.RegisterType(typeof(Tax));
        Assert.Equal("null", script.Run().ToDisplayString());
        Assert.Equal("0.08", engine.Compile("return Tax.Rate;").Run().ToDisplayString());
    }

    [Fact]
    public void EvaluateIsAOneLiner()
    {
        var engine = TestEngine.New();
        Assert.Equal("3", engine.Evaluate("return 1 + 2;").ToDisplayString());
        Assert.Equal("10", engine.Evaluate("return n * 2;",
            new Dictionary<string, object?> { ["n"] = 5 }).ToDisplayString());
    }

    // ---------------------------------------------------------------- 外部变量

    [Fact]
    public void ExternalsAreListedInOrderOfFirstUse()
        => Assert.Equal(["a", "b", "c"], TestEngine.New().Compile("return a + b * c;").Externals);

    [Fact]
    public void ExternalsAreDeduplicated()
        => Assert.Equal(["a"], TestEngine.New().Compile("return a + a + a;").Externals);

    [Fact]
    public void LocalsAreNotExternals()
        => Assert.Empty(TestEngine.New().Compile("let a = 1; return a;").Externals);

    [Fact]
    public void RegisteredTypesAreNotExternals()
        => Assert.Empty(TestEngine.New().Compile("return Tax.Rate;").Externals);

    [Fact]
    public void SlotOfReturnsMinusOneForUnknownName()
    {
        var script = TestEngine.New().Compile("return a;");
        Assert.True(script.SlotOf("a") >= 0);
        Assert.Equal(-1, script.SlotOf("nope"));
    }

    [Fact]
    public void MissingVariableBecomesNull()
        => Assert.Equal("null", TestEngine.New().Compile("return a;").Run().ToDisplayString());

    [Fact]
    public void ExtraVariablesAreIgnored()
        => Assert.Equal("1", TestEngine.New().Compile("return 1;")
            .Run(new Dictionary<string, object?> { ["unused"] = 99 }).ToDisplayString());

    [Fact]
    public void VariablesAcceptAllCommonClrTypes()
    {
        var script = TestEngine.New().Compile("return [i, l, d, f, s, b, n, e];");
        var r = script.Run(new Dictionary<string, object?>
        {
            ["i"] = 1,
            ["l"] = 2L,
            ["d"] = 3.5m,
            ["f"] = 4.5,
            ["s"] = "x",
            ["b"] = true,
            ["n"] = null,
            ["e"] = OrderState.Paid,
        });
        Assert.Equal("[1, 2, 3.5, 4.5, \"x\", true, null, \"Paid\"]", r.ToDisplayString());
    }

    // ---------------------------------------------------------------- 槽位快路径

    [Fact]
    public void CreateSlotsMatchesProgramSlotCount()
    {
        var script = TestEngine.New().Compile("let a = 1; let b = 2; return x + a + b;");
        Assert.Equal(script.Program.SlotCount, script.CreateSlots().Length);
    }

    [Fact]
    public void SlotPathAvoidsDictionaryLookup()
    {
        var script = TestEngine.New().Compile("return x * 2;");
        int sx = script.SlotOf("x");
        var slots = script.CreateSlots();

        for (int i = 0; i < 5; i++)
        {
            slots[sx] = Value.Int(i);
            Assert.Equal(i * 2, script.Run(slots).AsInt);
        }
    }

    [Fact]
    public void SlotsCanBeReusedAcrossRuns()
    {
        var script = TestEngine.New().Compile("let s = 0; for (let i = 0; i < n; i += 1) { s += i; } return s;");
        int sn = script.SlotOf("n");
        var slots = script.CreateSlots();

        slots[sn] = Value.Int(3);
        Assert.Equal(3L, script.Run(slots).AsInt);   // 0+1+2

        slots[sn] = Value.Int(5);
        Assert.Equal(10L, script.Run(slots).AsInt);  // 0+1+2+3+4，局部变量每次都会重新初始化
    }

    [Fact]
    public void TooSmallSlotArrayIsRejected()
    {
        var script = TestEngine.New().Compile("let a = 1; let b = 2; return a + b;");
        Assert.Throws<ArgumentException>(() => script.Run(new Value[1]));
    }

    [Fact]
    public void HostObjectsCanBePassedThroughSlots()
    {
        var script = TestEngine.New().Compile("return order.Total;");
        var slots = script.CreateSlots();
        slots[script.SlotOf("order")] = Value.Obj(TestEngine.SampleOrder());
        Assert.Equal("200", script.Run(slots).ToDisplayString());
    }

    // ---------------------------------------------------------------- 并发

    [Fact]
    public void SameProgramRunsConcurrently()
    {
        var script = TestEngine.New().Compile("let s = 0; for (let i = 0; i < 200; i += 1) { s += i * n; } return s;");
        int sn = script.SlotOf("n");

        Parallel.For(0, 64, _ =>
        {
            var slots = script.CreateSlots();     // 每个线程自己的槽位数组
            slots[sn] = Value.Int(2);
            Assert.Equal(39800L, script.Run(slots).AsInt);   // 2 * sum(0..199)
        });
    }

    [Fact]
    public void ConcurrentHostInteropIsStable()
    {
        var script = TestEngine.New().Compile("return order.Discount(0.1) + order.Tags.Count;");

        Parallel.For(0, 64, _ =>
        {
            var r = script.Run(new Dictionary<string, object?> { ["order"] = TestEngine.SampleOrder() });
            Assert.Equal("22.0", r.ToDisplayString());
        });
    }

    [Fact]
    public void ConcurrentCompilationIsStable()
    {
        var engine = TestEngine.New();
        Parallel.For(0, 64, i =>
        {
            var script = engine.GetOrCompile($"return {i % 8} + 1;");
            Assert.Equal($"{i % 8 + 1}", script.Run().ToDisplayString());
        });
    }

    // ---------------------------------------------------------------- 选项

    [Fact]
    public void DefaultOptions()
    {
        var o = new ScriptOptions();
        Assert.False(o.StrictVariables);
        Assert.Equal(20_000_000, o.Fuel);
    }

    [Fact]
    public void EngineExposesItsOptions()
    {
        var o = new ScriptOptions { Fuel = 123 };
        Assert.Same(o, new ScriptEngine(o).Options);
    }

    [Fact]
    public void ScriptInheritsEngineFuelAtCompileTime()
        => Assert.Equal(999, TestEngine.New(new ScriptOptions { Fuel = 999 }).Compile("return 1;").Fuel);

    // ---------------------------------------------------------------- 编译产物

    [Fact]
    public void DisassembleIsReadable()
    {
        var text = TestEngine.New().Compile("if (x > 1) { return x; } return 0;").Disassemble();
        Assert.Contains("LoadLocal", text);
        Assert.Contains("JumpIfFalse", text);
        Assert.Contains("Return", text);
        Assert.Contains("line", text);
    }

    [Fact]
    public void DisassembleAnnotatesConstantsAndSites()
    {
        var text = TestEngine.New().Compile("return order.Discount(0.5);").Disassemble();
        Assert.Contains("Discount/1", text);
        Assert.Contains("0.5", text);
    }

    [Fact]
    public void ProgramReportsMaxArgs()
    {
        Assert.Equal(0, TestEngine.New().Compile("return 1;").Program.MaxArgs);
        Assert.Equal(3, TestEngine.New().Compile("order.Join(\"a\", \"b\", \"c\");").Program.MaxArgs);
    }

    [Fact]
    public void LineMapCoversEveryInstruction()
    {
        var p = TestEngine.New().Compile("let a = 1;\nlet b = 2;\nreturn a + b;").Program;
        Assert.Equal(p.Code.Length, p.Lines.Length);
        Assert.Equal(p.Code.Length, p.Cols.Length);
        Assert.All(p.Lines, l => Assert.InRange(l, 1, 3));
    }

    [Fact]
    public void ProgramKeepsSource()
    {
        const string src = "return 1;";
        Assert.Equal(src, TestEngine.New().Compile(src).Program.Source);
    }

    // ---------------------------------------------------------------- 多脚本互不干扰

    [Fact]
    public void ScriptsFromOneEngineAreIndependent()
    {
        var engine = TestEngine.New();
        var a = engine.Compile("return x + 1;");
        var b = engine.Compile("return x * 10;");

        var vars = new Dictionary<string, object?> { ["x"] = 5 };
        Assert.Equal("6", a.Run(vars).ToDisplayString());
        Assert.Equal("50", b.Run(vars).ToDisplayString());
        Assert.Equal("6", a.Run(vars).ToDisplayString());
    }

    [Fact]
    public void RunningTheSameScriptRepeatedlyIsDeterministic()
    {
        var script = TestEngine.New().Compile("""
            let a = [];
            for (let i = 0; i < 3; i += 1) { a.Add(i * i); }
            return a;
            """);
        for (int i = 0; i < 20; i++) Assert.Equal("[0, 1, 4]", script.Run().ToDisplayString());
    }
}

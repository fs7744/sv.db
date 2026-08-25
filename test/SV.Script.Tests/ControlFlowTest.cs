namespace SV.Script.Tests;

public class ControlFlowTest
{
    private static string E(string src) => TestEngine.Eval(src);

    // ---------------------------------------------------------------- if / else

    [Theory]
    [InlineData("if (true) { return 1; } return 2;", "1")]
    [InlineData("if (false) { return 1; } return 2;", "2")]
    [InlineData("if (true) { return 1; } else { return 2; }", "1")]
    [InlineData("if (false) { return 1; } else { return 2; }", "2")]
    [InlineData("if (false) { return 1; } else if (true) { return 2; } else { return 3; }", "2")]
    [InlineData("if (false) { return 1; } else if (false) { return 2; } else { return 3; }", "3")]
    public void IfElse(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void LongElseIfChain()
    {
        const string src = """
            if (n == 1) { return "one"; }
            else if (n == 2) { return "two"; }
            else if (n == 3) { return "three"; }
            else if (n == 4) { return "four"; }
            else { return "many"; }
            """;
        var script = TestEngine.New().Compile(src);
        foreach (var (n, expected) in new[] { (1, "one"), (2, "two"), (3, "three"), (4, "four"), (9, "many") })
            Assert.Equal(expected, script.Run(new Dictionary<string, object?> { ["n"] = n }).ToDisplayString());
    }

    [Fact]
    public void BracesAreOptional()
    {
        Assert.Equal("1", E("if (true) return 1; else return 2;"));
        Assert.Equal("2", E("if (false) return 1; else return 2;"));
        Assert.Equal("3", E("if (false) return 1; return 3;"));
    }

    [Fact]
    public void ElseBindsToNearestIf()
    {
        // 悬垂 else 归属最近的 if
        Assert.Equal("2", E("if (true) if (false) return 1; else return 2; return 3;"));
        Assert.Equal("3", E("if (false) if (false) return 1; else return 2; return 3;"));
    }

    [Fact]
    public void NestedIf()
        => Assert.Equal("inner", E("""
            if (true) {
                if (true) {
                    return "inner";
                }
                return "outer-tail";
            }
            return "none";
            """));

    [Fact]
    public void IfConditionMustBeBool()
    {
        Assert.Contains("bool", TestEngine.Fails("if (1) { return 1; } return 2;").Message);
        Assert.Contains("bool", TestEngine.Fails("if (\"x\") { return 1; } return 2;").Message);
        Assert.Contains("bool", TestEngine.Fails("if (null) { return 1; } return 2;").Message);
    }

    // ---------------------------------------------------------------- 原始需求里的脚本

    [Theory]
    [InlineData(3, 2, 5, "10")]     // 1 < x < 6   -> y * c
    [InlineData(5, 2, 5, "10")]
    [InlineData(8, 20, 5, "4")]     // 6 < x < 12  -> y / c
    [InlineData(11, 20, 5, "4")]
    [InlineData(20, 2, 5, "6")]     // 其余        -> c + 1
    [InlineData(1, 2, 5, "6")]      // 边界不含
    [InlineData(6, 2, 5, "6")]      // 两段之间的空隙
    [InlineData(12, 2, 5, "6")]
    public void OriginalScript(int x, int y, int c, string expected)
    {
        const string src = """
            if (x > 1 and x < 6) {
                return y * c;
            } else if (x > 6 and x < 12) {
                return y / c;
            } else {
                return c + 1;
            }
            """;
        var r = TestEngine.New().Compile(src)
            .Run(new Dictionary<string, object?> { ["x"] = x, ["y"] = y, ["c"] = c });
        Assert.Equal(expected, r.ToDisplayString());
    }

    // ---------------------------------------------------------------- while

    [Fact]
    public void WhileAccumulates()
        => Assert.Equal("55", E("let s = 0; let i = 1; while (i <= 10) { s += i; i += 1; } return s;"));

    [Fact]
    public void WhileWithFalseConditionNeverRuns()
        => Assert.Equal("0", E("let n = 0; while (false) { n += 1; } return n;"));

    [Fact]
    public void WhileWithBreak()
        => Assert.Equal("5", E("let i = 0; while (true) { i += 1; if (i == 5) { break; } } return i;"));

    [Fact]
    public void WhileWithContinue()
        => Assert.Equal("4", E("let s = 0; let i = 0; while (i < 5) { i += 1; if (i == 3) { continue; } s += 1; } return s;"));

    [Fact]
    public void WhileBodyWithoutBraces()
        => Assert.Equal("3", E("let i = 0; while (i < 3) i += 1; return i;"));

    [Fact]
    public void WhileConditionMustBeBool()
        => Assert.Contains("bool", TestEngine.Fails("while (1) { break; } return 1;").Message);

    // ---------------------------------------------------------------- for

    [Fact]
    public void ForAccumulates()
        => Assert.Equal("55", E("let s = 0; for (let i = 1; i <= 10; i += 1) { s += i; } return s;"));

    [Fact]
    public void ForWithZeroIterations()
        => Assert.Equal("0", E("let n = 0; for (let i = 0; i < 0; i += 1) { n += 1; } return n;"));

    [Fact]
    public void ForWithoutInit()
        => Assert.Equal("3", E("let i = 0; for (; i < 3; i += 1) { } return i;"));

    [Fact]
    public void ForWithoutStep()
        => Assert.Equal("3", E("let n = 0; for (let i = 0; i < 3;) { n += 1; i += 1; } return n;"));

    [Fact]
    public void ForWithoutConditionNeedsBreak()
        => Assert.Equal("4", E("let i = 0; for (;;) { i += 1; if (i > 3) { break; } } return i;"));

    [Fact]
    public void ForWithExpressionInit()
        => Assert.Equal("3", E("let i = 0; for (i = 1; i < 3; i += 1) { } return i;"));

    [Fact]
    public void ForContinueStillRunsStep()
        => Assert.Equal("2", E("let s = 0; for (let i = 0; i < 5; i += 1) { if (i % 2 == 0) { continue; } s += 1; } return s;"));

    [Fact]
    public void ForBreakAndContinueTogether()
        => Assert.Equal("9", E("""
            let s = 0;
            for (let i = 0; i < 100; i += 1) {
                if (i % 2 == 0) { continue; }   // 跳过偶数
                if (i > 5) { break; }           // i = 7 时退出
                s += i;                         // 1 + 3 + 5
            }
            return s;
            """));

    [Fact]
    public void ForBodyWithoutBraces()
        => Assert.Equal("10", E("let s = 0; for (let i = 0; i < 5; i += 1) s += 2; return s;"));

    [Fact]
    public void NestedForBreakOnlyExitsInner()
        => Assert.Equal("9", E("""
            let n = 0;
            for (let i = 0; i < 3; i += 1) {
                for (let j = 0; j < 100; j += 1) {
                    if (j >= 3) { break; }
                    n += 1;
                }
            }
            return n;
            """));

    [Fact]
    public void NestedForContinueOnlyAffectsInner()
        => Assert.Equal("6", E("""
            let n = 0;
            for (let i = 0; i < 3; i += 1) {
                for (let j = 0; j < 4; j += 1) {
                    if (j % 2 == 1) { continue; }
                    n += 1;
                }
            }
            return n;
            """));

    [Fact]
    public void DeeplyNestedLoops()
        => Assert.Equal("27", E("""
            let n = 0;
            for (let a = 0; a < 3; a += 1) {
                for (let b = 0; b < 3; b += 1) {
                    for (let c = 0; c < 3; c += 1) { n += 1; }
                }
            }
            return n;
            """));

    // ---------------------------------------------------------------- foreach

    [Fact]
    public void ForeachOverArrayLiteral()
        => Assert.Equal("6", E("let s = 0; foreach (v in [1, 2, 3]) { s += v; } return s;"));

    [Fact]
    public void ForeachWithOptionalLet()
        => Assert.Equal("6", E("let s = 0; foreach (let v in [1, 2, 3]) { s += v; } return s;"));

    [Fact]
    public void ForeachOverEmptyArray()
        => Assert.Equal("0", E("let n = 0; foreach (v in []) { n += 1; } return n;"));

    [Fact]
    public void ForeachOverString()
        => Assert.Equal("a|b|c|", E("let s = \"\"; foreach (ch in \"abc\") { s += ch + \"|\"; } return s;"));

    [Fact]
    public void ForeachOverMapYieldsKeys()
        => Assert.Equal("ab", E("let m = { a: 1, b: 2 }; let s = \"\"; foreach (k in m) { s += k; } return s;"));

    [Fact]
    public void ForeachWithBreak()
        => Assert.Equal("3", E("let s = 0; foreach (v in [1, 2, 3, 4]) { if (v == 3) { break; } s += v; } return s;"));

    [Fact]
    public void ForeachWithContinue()
        => Assert.Equal("4", E("let s = 0; foreach (v in [1, 2, 3]) { if (v == 2) { continue; } s += v; } return s;"));

    [Fact]
    public void ForeachBodyWithoutBraces()
        => Assert.Equal("6", E("let s = 0; foreach (v in [1, 2, 3]) s += v; return s;"));

    [Fact]
    public void NestedForeach()
        => Assert.Equal("9", E("""
            let n = 0;
            foreach (a in [1, 2, 3]) {
                foreach (b in [1, 2, 3]) { n += 1; }
            }
            return n;
            """));

    [Fact]
    public void ForeachOverHostList()
        => Assert.Equal("redblue", TestEngine.EvalWith(
            "let s = \"\"; foreach (t in order.Tags) { s += t; } return s;", TestEngine.SampleOrder()));

    [Fact]
    public void ForeachOverHostReadOnlyList()
        => Assert.Equal("15", TestEngine.EvalWith(
            "let s = 0; foreach (n in order.Numbers) { s += n; } return s;", TestEngine.SampleOrder()));

    [Fact]
    public void ForeachOverHostDictionaryYieldsPairs()
    {
        // 宿主 Dictionary 的元素是 KeyValuePair，会被包成宿主对象
        var order = TestEngine.SampleOrder();
        order.Meta["k"] = 1;
        Assert.Equal("1", TestEngine.EvalWith(
            "let n = 0; foreach (kv in order.Meta) { n += 1; } return n;", order));
    }

    [Fact]
    public void ForeachOverNullThrows()
        => Assert.Contains("null", TestEngine.Fails("foreach (v in null) { } return 1;").Message);

    [Fact]
    public void ForeachOverNonIterableThrows()
        => Assert.Contains("不可迭代", TestEngine.Fails("foreach (v in 5) { } return 1;").Message);

    [Fact]
    public void ForeachVariableIsFresh()
        => Assert.Equal("3", E("let last = 0; foreach (v in [1, 2, 3]) { last = v; } return last;"));

    // ---------------------------------------------------------------- 块与作用域

    [Fact]
    public void InnerBlockCanShadowOuterVariable()
        => Assert.Equal("1", E("let x = 1; { let x = 2; } return x;"));

    [Fact]
    public void ShadowedValueIsVisibleInsideBlock()
        => Assert.Equal("2", E("let x = 1; { let x = 2; return x; } "));

    [Fact]
    public void OuterVariableIsWritableFromInnerBlock()
        => Assert.Equal("2", E("let x = 1; { x = 2; } return x;"));

    [Fact]
    public void RedeclaringInSameScopeIsCompileError()
        => Assert.Contains(TestEngine.FailsToCompile("let x = 1; let x = 2; return x;"),
            d => d.Message.Contains("已声明"));

    [Fact]
    public void RedeclaringInSiblingBlocksIsFine()
        => Assert.Equal("3", E("let s = 0; { let t = 1; s += t; } { let t = 2; s += t; } return s;"));

    [Fact]
    public void InitializerSeesOuterVariableNotItself()
        => Assert.Equal("2", E("let x = 1; { let x = x + 1; return x; }"));

    [Fact]
    public void LoopVariableIsNotVisibleAfterLoop()
    {
        // 非严格模式下 i 变成外部变量，值为 null；严格模式下是编译错误
        Assert.Equal("null", E("for (let i = 0; i < 3; i += 1) { } return i;"));

        var strict = new ScriptEngine(new ScriptOptions { StrictVariables = true });
        Assert.False(strict.TryCompile("for (let i = 0; i < 3; i += 1) { } return i;", out _, out var diags));
        Assert.Contains(diags, d => d.Message.Contains("'i'"));
    }

    [Fact]
    public void ForeachVariableIsNotVisibleAfterLoop()
        => Assert.Equal("null", E("foreach (v in [1]) { } return v;"));

    [Fact]
    public void BlockLocalIsNotVisibleOutside()
        => Assert.Equal("null", E("{ let a = 1; } return a;"));

    [Fact]
    public void ForInitVariableIsScopedToTheLoop()
        => Assert.Equal("3", E("let n = 0; for (let i = 0; i < 3; i += 1) { n += 1; } for (let i = 0; i < 0; i += 1) { } return n;"));

    // ---------------------------------------------------------------- return / 空语句

    [Fact]
    public void ReturnWithoutValueIsNull() => Assert.Equal("null", E("return;"));

    [Fact]
    public void FallingOffTheEndIsNull() => Assert.Equal("null", E("let x = 1;"));

    [Fact]
    public void EmptyScriptIsNull() => Assert.Equal("null", E(""));

    [Fact]
    public void OnlyCommentsIsNull() => Assert.Equal("null", E("// 啥也没有"));

    [Fact]
    public void EmptyStatementsAreAllowed() => Assert.Equal("1", E(";;; return 1; ;"));

    [Fact]
    public void EmptyBlockIsAllowed() => Assert.Equal("1", E("{ } { } return 1;"));

    [Fact]
    public void StatementsAfterReturnAreUnreachableButValid()
        => Assert.Equal("1", E("return 1; return 2;"));

    [Fact]
    public void ReturnInsideNestedLoopExitsEverything()
        => Assert.Equal("4", E("""
            for (let i = 0; i < 10; i += 1) {
                foreach (v in [1, 2, 3]) {
                    if (i == 2) { return i + 2; }
                }
            }
            return -1;
            """));

    // ---------------------------------------------------------------- break / continue 位置校验

    [Fact]
    public void BreakOutsideLoopIsCompileError()
        => Assert.Contains(TestEngine.FailsToCompile("break;"), d => d.Message.Contains("break"));

    [Fact]
    public void ContinueOutsideLoopIsCompileError()
        => Assert.Contains(TestEngine.FailsToCompile("continue;"), d => d.Message.Contains("continue"));

    [Fact]
    public void BreakInsideIfInsideLoopIsFine()
        => Assert.Equal("1", E("foreach (v in [1, 2]) { if (true) { break; } } return 1;"));

    [Fact]
    public void BreakInABlockNestedInLoopIsFine()
        => Assert.Equal("1", E("let n = 0; while (true) { { break; } } return 1;"));
}

using SV.Script.Runtime;

namespace SV.Script.Tests;

public class CollectionTest
{
    private static string E(string src) => TestEngine.Eval(src);

    // ---------------------------------------------------------------- 数组字面量

    [Theory]
    [InlineData("return [];", "[]")]
    [InlineData("return [1];", "[1]")]
    [InlineData("return [1, 2, 3];", "[1, 2, 3]")]
    [InlineData("return [1, 2, 3, ];", "[1, 2, 3]")]              // 允许尾逗号
    [InlineData("return [\"a\", \"b\"];", "[\"a\", \"b\"]")]
    [InlineData("return [1, \"a\", true, null];", "[1, \"a\", true, null]")]  // 元素类型可混
    [InlineData("return [1 + 1, 2 * 2];", "[2, 4]")]              // 元素是表达式
    [InlineData("return [[1, 2], [3]];", "[[1, 2], [3]]")]        // 嵌套
    public void ArrayLiteral(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void ArrayLiteralKindIsArray()
        => Assert.Equal(ValueKind.Array, TestEngine.RunVars("return [1];").Kind);

    // ---------------------------------------------------------------- 数组下标

    [Theory]
    [InlineData("let a = [10, 20, 30]; return a[0];", "10")]
    [InlineData("let a = [10, 20, 30]; return a[2];", "30")]
    [InlineData("let a = [10, 20, 30]; return a[1 + 1];", "30")]
    [InlineData("let a = [10, 20, 30]; return a[4 / 2];", "30")]  // 无小数部分的 decimal 可作下标
    [InlineData("let a = [[1, 2]]; return a[0][1];", "2")]        // 链式下标
    public void ArrayIndexRead(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void ArrayIndexWrite()
        => Assert.Equal("[9, 20]", E("let a = [10, 20]; a[0] = 9; return a;"));

    [Fact]
    public void ArrayCompoundAssign()
    {
        Assert.Equal("15", E("let a = [10]; a[0] += 5; return a[0];"));
        Assert.Equal("5", E("let a = [10]; a[0] -= 5; return a[0];"));
        Assert.Equal("20", E("let a = [10]; a[0] *= 2; return a[0];"));
        Assert.Equal("5", E("let a = [10]; a[0] /= 2; return a[0];"));
        Assert.Equal("1", E("let a = [10]; a[0] %= 3; return a[0];"));
    }

    [Fact]
    public void ArrayCompoundAssignEvaluatesTargetOnce()
        => Assert.Equal("[11, 20]", E("let a = [10, 20]; let i = 0; a[i] += 1; return a;"));

    [Theory]
    [InlineData("let a = [1]; return a[1];")]
    [InlineData("let a = [1]; return a[-1];")]
    [InlineData("let a = []; return a[0];")]
    public void ArrayIndexOutOfRangeThrows(string src)
        => Assert.Contains("下标越界", TestEngine.Fails(src).Message);

    [Fact]
    public void ArrayIndexMustBeInteger()
    {
        Assert.Contains("下标必须是整数", TestEngine.Fails("let a = [1]; return a[0.5];").Message);
        Assert.Contains("下标必须是整数", TestEngine.Fails("let a = [1]; return a[\"x\"];").Message);
    }

    // ---------------------------------------------------------------- 数组成员

    [Fact]
    public void ArrayCount()
        => Assert.Equal("3", E("return [1, 2, 3].Count;"));

    [Fact]
    public void ArrayAdd()
        => Assert.Equal("[1, 2]", E("let a = [1]; a.Add(2); return a;"));

    [Fact]
    public void ArrayAddThenCount()
        => Assert.Equal("3", E("let a = [1, 2]; a.Add(9); return a.Count;"));

    [Fact]
    public void ArrayInsertAndRemoveAt()
        => Assert.Equal("[0, 2]", E("let a = [1, 2]; a.Insert(0, 0); a.RemoveAt(1); return a;"));

    [Fact]
    public void ArrayClear()
        => Assert.Equal("0", E("let a = [1, 2]; a.Clear(); return a.Count;"));

    [Fact]
    public void ArrayContainsAndIndexOf()
    {
        Assert.Equal("true", E("return [1, 2].Contains(2);"));
        Assert.Equal("false", E("return [1, 2].Contains(9);"));
        Assert.Equal("1", E("return [1, 2].IndexOf(2);"));
        Assert.Equal("-1", E("return [1, 2].IndexOf(9);"));
    }

    [Fact]
    public void ArrayContainsUsesScriptEquality()
        => Assert.Equal("true", E("return [1].Contains(1.0);"));

    [Fact]
    public void ArrayRemoveAtOutOfRangeThrows()
        => Assert.Contains("下标越界", TestEngine.Fails("let a = [1]; a.RemoveAt(5); return 1;").Message);

    [Fact]
    public void UnknownArrayMemberThrows()
        => Assert.Contains("Nope", TestEngine.Fails("return [1].Nope;").Message);

    // ---------------------------------------------------------------- 字典字面量

    [Theory]
    [InlineData("return { };", "{}")]
    [InlineData("return { a: 1 };", "{a: 1}")]
    [InlineData("return { a: 1, b: 2 };", "{a: 1, b: 2}")]
    [InlineData("return { a: 1, b: 2, };", "{a: 1, b: 2}")]           // 允许尾逗号
    [InlineData("return { \"with space\": 1 };", "{with space: 1}")]  // 字符串键
    [InlineData("return { a: \"s\" };", "{a: \"s\"}")]
    [InlineData("return { a: 1 + 1 };", "{a: 2}")]
    [InlineData("return { a: { b: 1 } };", "{a: {b: 1}}")]            // 嵌套
    public void MapLiteral(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void MapLiteralKindIsMap()
        => Assert.Equal(ValueKind.Map, TestEngine.RunVars("return { a: 1 };").Kind);

    [Fact]
    public void MapLiteralIsOnlyValidInExpressionPosition()
    {
        // 语句开头的 { 一律当作块，所以裸的字典字面量语句不成立
        Assert.NotEmpty(TestEngine.FailsToCompile("{ a: 1 };"));
        Assert.Equal("{a: 1}", E("let m = { a: 1 }; return m;"));
    }

    // ---------------------------------------------------------------- 字典访问

    [Theory]
    [InlineData("let m = { a: 1 }; return m.a;", "1")]
    [InlineData("let m = { a: 1 }; return m[\"a\"];", "1")]
    [InlineData("let m = { a: 1 }; return m.zzz;", "null")]                 // 缺键读作 null
    [InlineData("let m = { a: 1 }; return m[\"zzz\"];", "null")]
    [InlineData("let m = { a: { b: 2 } }; return m.a.b;", "2")]             // 嵌套读
    [InlineData("let m = { a: 1 }; return m.Count;", "1")]                  // 键不存在时退到 ScriptMap 成员
    public void MapRead(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void MapKeyShadowsMapMember()
    {
        // 键优先于 ScriptMap 自身成员，这是刻意的：脚本作者的键更重要
        Assert.Equal("99", E("let m = { Count: 99 }; return m.Count;"));
    }

    [Fact]
    public void MapWrite()
    {
        Assert.Equal("7", E("let m = { }; m.x = 7; return m.x;"));
        Assert.Equal("7", E("let m = { }; m[\"x\"] = 7; return m[\"x\"];"));
        Assert.Equal("2", E("let m = { a: 1 }; m.a = 2; return m.a;"));
    }

    [Fact]
    public void MapCompoundAssign()
    {
        Assert.Equal("3", E("let m = { a: 1 }; m.a += 2; return m.a;"));
        Assert.Equal("3", E("let m = { a: 1 }; m[\"a\"] += 2; return m[\"a\"];"));
    }

    [Fact]
    public void MapCompoundAssignOnMissingKeyThrows()
        => Assert.Contains("不支持", TestEngine.Fails("let m = { }; m.a += 1; return m.a;").Message);

    [Fact]
    public void MapIndexKeyMustBeString()
        => Assert.Contains("键必须是字符串", TestEngine.Fails("let m = { }; return m[1];").Message);

    [Fact]
    public void MapMembers()
    {
        Assert.Equal("2", E("let m = { a: 1, b: 2 }; return m.Count;"));
        Assert.Equal("true", E("let m = { a: 1 }; return m.ContainsKey(\"a\");"));
        Assert.Equal("false", E("let m = { a: 1 }; return m.ContainsKey(\"z\");"));
        Assert.Equal("true", E("let m = { a: 1 }; return m.Remove(\"a\");"));
        Assert.Equal("0", E("let m = { a: 1 }; m.Clear(); return m.Count;"));
        Assert.Equal("[\"a\", \"b\"]", E("let m = { a: 1, b: 2 }; return m.Keys();"));
        Assert.Equal("[1, 2]", E("let m = { a: 1, b: 2 }; return m.Values();"));
    }

    [Fact]
    public void MapPreservesInsertionOrderForKeys()
        => Assert.Equal("[\"z\", \"a\", \"m\"]", E("let m = { z: 1, a: 2, m: 3 }; return m.Keys();"));

    // ---------------------------------------------------------------- 混合与实战

    [Fact]
    public void ArrayOfMaps()
        => Assert.Equal("b", E("let rows = [{ n: \"a\" }, { n: \"b\" }]; return rows[1].n;"));

    [Fact]
    public void MapOfArrays()
        => Assert.Equal("2", E("let m = { xs: [1, 2] }; return m.xs[1];"));

    [Fact]
    public void BuildArrayInLoop()
        => Assert.Equal("[0, 1, 2]", E("let a = []; for (let i = 0; i < 3; i += 1) { a.Add(i); } return a;"));

    [Fact]
    public void BuildMapInLoop()
        => Assert.Equal("3", E("""
            let m = { };
            foreach (k in ["a", "b", "c"]) { m[k] = 1; }
            return m.Count;
            """));

    [Fact]
    public void SumOverNestedStructure()
        => Assert.Equal("10", E("""
            let rows = [{ qty: 1 }, { qty: 4 }, { qty: 5 }];
            let total = 0;
            foreach (r in rows) { total += r.qty; }
            return total;
            """));

    [Fact]
    public void FilterAndAggregateWithoutLambda()
        => Assert.Equal("2", E("""
            let rows = [{ ok: true }, { ok: false }, { ok: true }];
            let n = 0;
            foreach (r in rows) {
                if (not r.ok) { continue; }
                n += 1;
            }
            return n;
            """));

    // ---------------------------------------------------------------- 引用语义

    [Fact]
    public void ArraysAreReferences()
        => Assert.Equal("[1, 2]", E("let a = [1]; let b = a; b.Add(2); return a;"));

    [Fact]
    public void MapsAreReferences()
        => Assert.Equal("2", E("let m = { a: 1 }; let n = m; n.a = 2; return m.a;"));

    [Fact]
    public void NullMemberOnNullMapThrows()
        => Assert.Contains("null", TestEngine.Fails("let m = null; return m.a;").Message);

    [Fact]
    public void IndexingNullThrows()
        => Assert.Contains("null", TestEngine.Fails("let m = null; return m[0];").Message);

    // ---------------------------------------------------------------- 字符串下标

    [Fact]
    public void StringIndexReturnsSingleCharString()
        => Assert.Equal("e", E("return \"hey\"[1];"));

    [Fact]
    public void StringIndexOutOfRangeThrows()
        => Assert.Contains("越界", TestEngine.Fails("return \"a\"[5];").Message);

    [Fact]
    public void StringIsImmutable()
        => Assert.Contains("不可修改", TestEngine.Fails("let s = \"a\"; s[0] = \"b\"; return s;").Message);
}

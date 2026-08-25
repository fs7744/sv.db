using SV.Script.Runtime;

namespace SV.Script.Tests;

public class ArithmeticTest
{
    private static string E(string src) => TestEngine.Eval(src);

    // ---------------------------------------------------------------- 字面量

    [Theory]
    [InlineData("return 0;", "0")]
    [InlineData("return 42;", "42")]
    [InlineData("return 1.5;", "1.5")]
    [InlineData("return 0.001;", "0.001")]
    [InlineData("return true;", "true")]
    [InlineData("return false;", "false")]
    [InlineData("return null;", "null")]
    [InlineData("return \"txt\";", "txt")]
    [InlineData("return '单引号';", "单引号")]
    [InlineData("return 9223372036854775807;", "9223372036854775807")]
    public void Literals(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void IntegerAndDecimalLiteralsStaySeparateConstants()
    {
        // 常量池去重必须按 Kind 区分，否则 1 和 1.0 会被合并成同一个常量
        Assert.Equal("1.0", E("let a = 1; let b = 1.0; return b;"));
        Assert.Equal("1", E("let a = 1.0; let b = 1; return b;"));
    }

    [Fact]
    public void DecimalScaleIsPreserved()
    {
        Assert.Equal("1.50", E("return 1.50;"));
        Assert.Equal("1.5", E("return 1.5;"));
    }

    // ---------------------------------------------------------------- 优先级与结合性

    [Theory]
    [InlineData("return 1 + 2 * 3;", "7")]
    [InlineData("return 2 * 3 + 1;", "7")]
    [InlineData("return (1 + 2) * 3;", "9")]
    [InlineData("return 1 + 2 - 3 + 4;", "4")]
    [InlineData("return 2 - 3 - 4;", "-5")]                  // 减法左结合
    [InlineData("return 100 / 10 / 2;", "5")]                // 除法左结合
    [InlineData("return 2 + 3 * 4 - 6 / 3;", "12")]
    [InlineData("return 10 % 4 * 2;", "4")]                  // % 与 * 同级，左结合
    [InlineData("return -2 * 3;", "-6")]                     // 一元优先于二元
    [InlineData("return -(2 * 3);", "-6")]
    [InlineData("return 1 < 2 == true;", "true")]            // 比较紧于相等
    [InlineData("return not false and true;", "true")]       // not 紧于 and
    [InlineData("return false and false or true;", "true")]  // and 紧于 or
    [InlineData("return true or false and false;", "true")]
    public void PrecedenceAndAssociativity(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void ConditionalIsRightAssociative()
    {
        Assert.Equal("b", E("return false ? \"a\" : true ? \"b\" : \"c\";"));
        Assert.Equal("c", E("return false ? \"a\" : false ? \"b\" : \"c\";"));
    }

    [Fact]
    public void CoalesceIsRightAssociative()
        => Assert.Equal("z", E("return null ?? null ?? \"z\";"));

    [Fact]
    public void UnaryPlusIsAccepted() => Assert.Equal("3", E("return +3;"));

    [Fact]
    public void DoubleNegationCancels() => Assert.Equal("5", E("return - -5;"));

    [Fact]
    public void NotCanStack() => Assert.Equal("true", E("return !!true;"));

    // ---------------------------------------------------------------- long 快路径与溢出提升

    [Theory]
    [InlineData("return 2 + 3;", "5")]
    [InlineData("return 3000000000 * 3;", "9000000000")]     // 仍在 long 范围内
    [InlineData("return 4000000000 * 4000000000;", "16000000000000000000")] // 溢出 -> decimal
    [InlineData("return 9223372036854775807 + 1;", "9223372036854775808")]
    [InlineData("return 9223372036854775807 * 2;", "18446744073709551614")]
    public void LongFastPathAndOverflow(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void LongUnderflowPromotes()
    {
        var r = TestEngine.RunVars("return v - 1;", ("v", long.MinValue));
        Assert.Equal(ValueKind.Dec, r.Kind);
        Assert.Equal("-9223372036854775809", r.ToDisplayString());
    }

    [Fact]
    public void NegatingLongMinValuePromotes()
    {
        var r = TestEngine.RunVars("return -v;", ("v", long.MinValue));
        Assert.Equal(ValueKind.Dec, r.Kind);
        Assert.Equal("9223372036854775808", r.ToDisplayString());
    }

    [Fact]
    public void MultiplyByZeroStaysInteger()
    {
        var r = TestEngine.RunVars("return v * 0;", ("v", long.MaxValue));
        Assert.Equal(ValueKind.Int, r.Kind);
        Assert.Equal("0", r.ToDisplayString());
    }

    [Fact]
    public void IntegerResultsKeepIntKind()
    {
        Assert.Equal(ValueKind.Int, TestEngine.RunVars("return 1 + 2;").Kind);
        Assert.Equal(ValueKind.Int, TestEngine.RunVars("return 7 % 3;").Kind);
        Assert.Equal(ValueKind.Dec, TestEngine.RunVars("return 6 / 3;").Kind); // 除法始终 decimal
    }

    // ---------------------------------------------------------------- 除法与取模

    [Theory]
    [InlineData("return 5 / 2;", "2.5")]
    [InlineData("return 6 / 3;", "2")]
    [InlineData("return 1 / 4;", "0.25")]
    [InlineData("return -7 / 2;", "-3.5")]
    [InlineData("return 7 % 3;", "1")]
    [InlineData("return -7 % 3;", "-1")]
    [InlineData("return 7 % -3;", "1")]
    [InlineData("return 7.5 % 2;", "1.5")]
    [InlineData("return 0 % 5;", "0")]
    public void DivisionAndModulo(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void DivideByZeroThrows()
        => Assert.Contains("除数为 0", TestEngine.Fails("return 1 / 0;").Message);

    [Fact]
    public void DivideByDecimalZeroThrows()
        => Assert.Contains("除数为 0", TestEngine.Fails("return 1 / 0.0;").Message);

    [Fact]
    public void ModuloByZeroThrows()
        => Assert.Contains("取模的除数为 0", TestEngine.Fails("return 1 % 0;").Message);

    [Fact]
    public void LongMinValueModuloMinusOneDoesNotOverflow()
        => Assert.Equal("0", TestEngine.RunVars("return v % -1;", ("v", long.MinValue)).ToDisplayString());

    // ---------------------------------------------------------------- decimal 精度

    [Fact]
    public void DecimalArithmeticIsExact()
    {
        // 这正是选 decimal 而不是 double 的理由
        Assert.Equal("0.3", E("return 0.1 + 0.2;"));
        Assert.Equal("0.00", E("return 1.10 - 1.10;"));
        Assert.Equal("100.00", E("return 0.01 * 10000;"));
    }

    [Fact]
    public void MixedIntAndDecimalPromotes()
    {
        Assert.Equal("160.0", E("return 200 * 0.8;"));
        Assert.Equal("2.5", E("return 2 + 0.5;"));
        Assert.Equal("1.5", E("return 2 - 0.5;"));
    }

    [Fact]
    public void RepeatingDivisionKeepsDecimalPrecision()
    {
        var r = TestEngine.RunVars("return 1 / 3;");
        Assert.Equal(ValueKind.Dec, r.Kind);
        Assert.Equal(1m / 3m, r.AsDec);
    }

    // ---------------------------------------------------------------- 字符串

    [Theory]
    [InlineData("return \"a\" + \"b\";", "ab")]
    [InlineData("return \"a\" + 1;", "a1")]
    [InlineData("return 1 + \"a\";", "1a")]
    [InlineData("return \"n=\" + 1.50;", "n=1.50")]
    [InlineData("return \"b=\" + true;", "b=true")]
    [InlineData("return \"x\" + null;", "xnull")]
    [InlineData("return \"a\" + [1, 2];", "a[1, 2]")]
    [InlineData("return \"\" + 1 + 2;", "12")]              // 左结合，先变字符串
    [InlineData("return 1 + 2 + \"\";", "3")]               // 先算数字再拼
    public void StringConcat(string src, string expected) => Assert.Equal(expected, E(src));

    [Theory]
    [InlineData("return \"a\" < \"b\";", "true")]
    [InlineData("return \"b\" < \"a\";", "false")]
    [InlineData("return \"B\" < \"a\";", "true")]           // 序数比较，大写在前
    [InlineData("return \"abc\" == \"abc\";", "true")]
    [InlineData("return \"abc\" != \"abd\";", "true")]
    [InlineData("return \"\" <= \"\";", "true")]
    public void StringComparison(string src, string expected) => Assert.Equal(expected, E(src));

    // ---------------------------------------------------------------- 相等与比较

    [Theory]
    [InlineData("return 1 == 1;", "true")]
    [InlineData("return 1 == 1.0;", "true")]                 // 数字跨表示相等
    [InlineData("return 1.0 == 1;", "true")]
    [InlineData("return 1 != 2;", "true")]
    [InlineData("return null == null;", "true")]
    [InlineData("return null != 1;", "true")]
    [InlineData("return 1 != null;", "true")]
    [InlineData("return true == true;", "true")]
    [InlineData("return true != false;", "true")]
    [InlineData("return \"1\" == 1;", "false")]              // 不做隐式转换
    [InlineData("return true == 1;", "false")]
    [InlineData("return 1 < 1.5;", "true")]
    [InlineData("return 2 >= 2.0;", "true")]
    [InlineData("return -1 < 0;", "true")]
    public void EqualityAndComparison(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void ArrayEqualityIsByReference()
    {
        Assert.Equal("false", E("return [1] == [1];"));
        Assert.Equal("true", E("let a = [1]; let b = a; return a == b;"));
    }

    [Fact]
    public void ComparingDifferentTypesThrows()
    {
        Assert.Contains("无法比较", TestEngine.Fails("return 1 < \"a\";").Message);
        Assert.Contains("无法比较", TestEngine.Fails("return null < 1;").Message);
        Assert.Contains("无法比较", TestEngine.Fails("return true > false;").Message);
    }

    // ---------------------------------------------------------------- 类型错误

    [Theory]
    [InlineData("return null + 1;")]
    [InlineData("return true + 1;")]
    [InlineData("return [1] - 1;")]
    [InlineData("return 1 * null;")]
    [InlineData("return \"a\" - \"b\";")]
    [InlineData("return \"a\" * 2;")]
    public void UnsupportedOperandsThrow(string src)
        => Assert.Contains("不支持", TestEngine.Fails(src).Message);

    [Fact]
    public void NegatingNonNumberThrows()
        => Assert.Contains("一元 -", TestEngine.Fails("return -\"a\";").Message);

    [Fact]
    public void NotOnNonBoolThrows()
        => Assert.Contains("bool", TestEngine.Fails("return !1;").Message);

    // ---------------------------------------------------------------- 逻辑运算与短路

    [Theory]
    [InlineData("return true and true;", "true")]
    [InlineData("return true and false;", "false")]
    [InlineData("return false and true;", "false")]
    [InlineData("return true or false;", "true")]
    [InlineData("return false or false;", "false")]
    [InlineData("return true && true;", "true")]
    [InlineData("return false || true;", "true")]
    public void LogicalOperators(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void AndShortCircuitsRight()
        => Assert.Equal("false", E("return false and (1 / 0) == 0;"));

    [Fact]
    public void OrShortCircuitsRight()
        => Assert.Equal("true", E("return true or (1 / 0) == 0;"));

    [Fact]
    public void ShortCircuitSkipsHostCall()
    {
        // order.Boom() 会抛异常；能正常返回就说明右侧确实没被执行
        Assert.Equal("false",
            TestEngine.EvalWith("return false and order.Boom() == null;", TestEngine.SampleOrder()));
        Assert.Equal("true",
            TestEngine.EvalWith("return true or order.Boom() == null;", TestEngine.SampleOrder()));
    }

    [Fact]
    public void AssignmentIsNotAnExpression()
    {
        // 赋值只作为语句存在，避免 if (a = b) 这类经典笔误
        Assert.NotEmpty(TestEngine.FailsToCompile("let n = 0; return (n = 1);"));
        Assert.NotEmpty(TestEngine.FailsToCompile("let n = 0; if (n = 1) { return 1; }"));
    }

    [Fact]
    public void LogicalOperandsMustBeBool()
    {
        Assert.Contains("and 的左侧", TestEngine.Fails("return 1 and true;").Message);
        Assert.Contains("and 的右侧", TestEngine.Fails("return true and 1;").Message);
        Assert.Contains("or 的左侧", TestEngine.Fails("return 1 or true;").Message);
        Assert.Contains("or 的右侧", TestEngine.Fails("return false or 1;").Message);
    }

    // ---------------------------------------------------------------- 三元与空合并

    [Theory]
    [InlineData("return true ? 1 : 2;", "1")]
    [InlineData("return false ? 1 : 2;", "2")]
    [InlineData("return 1 > 2 ? \"a\" : \"b\";", "b")]
    [InlineData("return null ?? 5;", "5")]
    [InlineData("return 3 ?? 5;", "3")]
    [InlineData("return false ?? 5;", "false")]             // false 不是 null
    [InlineData("return 0 ?? 5;", "0")]                     // 0 不是 null
    public void ConditionalAndCoalesce(string src, string expected) => Assert.Equal(expected, E(src));

    [Fact]
    public void ConditionalOnlyEvaluatesTakenBranch()
        => Assert.Equal("1", E("return true ? 1 : 1 / 0;"));

    [Fact]
    public void CoalesceOnlyEvaluatesRightWhenNull()
        => Assert.Equal("1", E("return 1 ?? 1 / 0;"));

    [Fact]
    public void ConditionalRequiresBoolCondition()
        => Assert.Contains("bool", TestEngine.Fails("return 1 ? 2 : 3;").Message);
}

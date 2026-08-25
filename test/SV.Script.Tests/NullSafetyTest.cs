namespace SV.Script.Tests;

public class NullSafetyTest
{
    private static Order OrderWithoutCustomer()
    {
        var o = TestEngine.SampleOrder();
        o.Customer = null;
        o.Ship = null;
        return o;
    }

    private static string W(string src, object? order) => TestEngine.EvalWith(src, order);

    // ---------------------------------------------------------------- ?. 基本行为

    [Fact]
    public void ConditionalMemberOnNonNullBehavesNormally()
        => Assert.Equal("Ann", W("return order.Customer?.Name;", TestEngine.SampleOrder()));

    [Fact]
    public void ConditionalMemberOnNullYieldsNull()
        => Assert.Equal("null", W("return order.Customer?.Name;", OrderWithoutCustomer()));

    [Fact]
    public void ConditionalMemberShortCircuitsWholeChain()
    {
        // C# 语义：前面为 null 时整条链都不再求值
        Assert.Equal("null", W("return order.Customer?.Name.ToUpperInvariant();", OrderWithoutCustomer()));
        Assert.Equal("ANN", W("return order.Customer?.Name.ToUpperInvariant();", TestEngine.SampleOrder()));
    }

    [Fact]
    public void ConditionalMemberShortCircuitsIndexing()
    {
        Assert.Equal("null", W("return order.Customer?.Name[0];", OrderWithoutCustomer()));
        Assert.Equal("A", W("return order.Customer?.Name[0];", TestEngine.SampleOrder()));
    }

    [Fact]
    public void ConditionalMethodCall()
    {
        Assert.Equal("null", W("return order.Ship?.Region;", OrderWithoutCustomer()));
        Assert.Equal("CA", W("return order.Ship?.Region;", TestEngine.SampleOrder()));
    }

    [Fact]
    public void MultipleConditionalLinksInOneChain()
    {
        var o = TestEngine.SampleOrder();
        Assert.Equal("null", W("return order.Customer?.Referrer?.Name;", o));

        o.Customer!.Referrer = new Customer { Name = "Bob" };
        Assert.Equal("Bob", W("return order.Customer?.Referrer?.Name;", o));
    }

    [Fact]
    public void ConditionalOnMapMissingKey()
        => Assert.Equal("null", TestEngine.Eval("let m = { }; return m.a?.b;"));

    [Fact]
    public void ConditionalChainInsideLargerExpression()
    {
        Assert.Equal("anon", W("return order.Customer?.Name ?? \"anon\";", OrderWithoutCustomer()));
        Assert.Equal("Ann", W("return order.Customer?.Name ?? \"anon\";", TestEngine.SampleOrder()));
    }

    [Fact]
    public void ConditionalChainUsedInCondition()
        => Assert.Equal("no", W("""
            if (order.Customer?.IsVip == true) { return "yes"; }
            return "no";
            """, OrderWithoutCustomer()));

    [Fact]
    public void ConditionalChainAsMethodArgument()
        => Assert.Equal("fallback", W(
            "return order.Customer?.Name ?? \"fallback\";", OrderWithoutCustomer()));

    [Fact]
    public void ConditionalCannotBeAssignmentTarget()
        => Assert.Contains(TestEngine.FailsToCompile("order.Customer?.Name = \"x\";"),
            d => d.Message.Contains("?."));

    // ---------------------------------------------------------------- ?? 空合并

    [Theory]
    [InlineData("return null ?? 1;", "1")]
    [InlineData("return null ?? null ?? 2;", "2")]
    [InlineData("return 1 ?? 2;", "1")]
    [InlineData("return \"\" ?? \"x\";", "")]          // 空串不是 null
    [InlineData("return false ?? true;", "false")]     // false 不是 null
    [InlineData("return 0 ?? 1;", "0")]                // 0 不是 null
    public void Coalesce(string src, string expected) => Assert.Equal(expected, TestEngine.Eval(src));

    [Fact]
    public void CoalesceDoesNotEvaluateRightWhenLeftIsPresent()
        => Assert.Equal("1", TestEngine.Eval("return 1 ?? 1 / 0;"));

    [Fact]
    public void CoalesceWithHostNullReturn()
        => Assert.Equal("dflt", W("return order.MaybeNull() ?? \"dflt\";", TestEngine.SampleOrder()));

    [Fact]
    public void CoalesceOnMissingMapKey()
        => Assert.Equal("5", TestEngine.Eval("let m = { }; return m.k ?? 5;"));

    [Fact]
    public void CoalesceOnNullableHostProperty()
        => Assert.Equal("1", W("return order.Priority ?? 1;", TestEngine.SampleOrder()));

    // ---------------------------------------------------------------- 不用 ?. 时的错误信息

    [Fact]
    public void PlainMemberAccessOnNullThrows()
    {
        var ex = TestEngine.Fails("return order.Customer.Name;", OrderWithoutCustomer());
        Assert.Contains("null", ex.Message);
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void PlainMethodCallOnNullThrows()
    {
        var ex = TestEngine.Fails("return order.Customer.ToString();", OrderWithoutCustomer());
        Assert.Contains("null", ex.Message);
        Assert.Contains("ToString", ex.Message);
    }

    [Fact]
    public void PlainWriteOnNullThrows()
        => Assert.Contains("null", TestEngine.Fails("order.Customer.Name = \"x\"; return 1;", OrderWithoutCustomer()).Message);

    [Fact]
    public void PlainIndexOnNullThrows()
        => Assert.Contains("null", TestEngine.Fails("return order.Customer[0];", OrderWithoutCustomer()).Message);

    [Fact]
    public void MissingExternalVariableIsNull()
        => Assert.Equal("null", TestEngine.Eval("return whatever;"));

    [Fact]
    public void MemberOnMissingExternalVariableThrows()
        => Assert.Contains("null", TestEngine.Fails("return whatever.Prop;").Message);

    [Fact]
    public void NullPropagatesThroughCoalesceGuardPattern()
        => Assert.Equal("0", W("""
            let c = order.Customer;
            if (c == null) { return 0; }
            return c.IsVip ? 1 : 2;
            """, OrderWithoutCustomer()));

    [Fact]
    public void NullCheckWithNotEquals()
        => Assert.Equal("1", W("""
            let c = order.Customer;
            if (c != null and c.IsVip) { return 1; }
            return 0;
            """, TestEngine.SampleOrder()));

    [Fact]
    public void AndShortCircuitProtectsNullDereference()
        => Assert.Equal("0", W("""
            if (order.Customer != null and order.Customer.IsVip) { return 1; }
            return 0;
            """, OrderWithoutCustomer()));
}

using SV.Script.Runtime;

namespace SV.Script.Tests;

public class InteropTest
{
    private static string W(string src, object? order = null)
        => TestEngine.EvalWith(src, order ?? TestEngine.SampleOrder());

    private static string V(string src, params (string, object?)[] vars)
        => TestEngine.RunVars(src, vars).ToDisplayString();

    // ---------------------------------------------------------------- 属性读

    [Theory]
    [InlineData("return order.Code;", "X-9")]
    [InlineData("return order.Total;", "200")]
    [InlineData("return order.Count;", "2")]
    [InlineData("return order.Rush;", "false")]
    [InlineData("return order.Weight;", "1.5")]                     // double -> decimal
    [InlineData("return order.Customer.Name;", "Ann")]
    [InlineData("return order.Customer.IsVip;", "true")]
    [InlineData("return order.Ship.City;", "Irvine")]
    [InlineData("return order.Id;", "7")]                           // 继承自基类的属性
    [InlineData("return order.Kind;", "order")]                     // 虚属性走运行期类型
    [InlineData("return order.ShipAsInterface.Region;", "CA")]      // 接口类型声明，实际类型解析
    [InlineData("return order.PaidAt;", "null")]                    // 可空且为 null
    [InlineData("return order.Priority;", "null")]
    public void ReadProperty(string src, string expected) => Assert.Equal(expected, W(src));

    [Fact]
    public void DeepMemberChain()
    {
        var order = TestEngine.SampleOrder();
        order.Customer!.Referrer = new Customer { Name = "Bob" };
        Assert.Equal("Bob", W("return order.Customer.Referrer.Name;", order));
    }

    [Fact]
    public void ReadField()
        => Assert.Equal("5", W("return order.PlainField;"));

    [Fact]
    public void ReadReadonlyField()
        => Assert.Equal("42", W("return order.ReadonlyField;"));

    // ---------------------------------------------------------------- 属性写

    [Fact]
    public void WriteStringProperty()
        => Assert.Equal("Z", W("order.Code = \"Z\"; return order.Code;"));

    [Fact]
    public void WriteDecimalProperty()
        => Assert.Equal("50", W("order.Total = 50; return order.Total;"));

    [Fact]
    public void WriteIntPropertyFromDecimalWithNoFraction()
        => Assert.Equal("4", W("order.Count = 8 / 2; return order.Count;"));

    [Fact]
    public void WriteDoubleProperty()
        => Assert.Equal("2.5", W("order.Weight = 2.5; return order.Weight;"));

    [Fact]
    public void WriteBoolProperty()
        => Assert.Equal("true", W("order.Rush = true; return order.Rush;"));

    [Fact]
    public void WriteNestedProperty()
        => Assert.Equal("Cara", W("order.Customer.Name = \"Cara\"; return order.Customer.Name;"));

    [Fact]
    public void WriteField()
        => Assert.Equal("9", W("order.PlainField = 9; return order.PlainField;"));

    [Fact]
    public void CompoundAssignOnProperty()
    {
        Assert.Equal("X-9!", W("order.Code += \"!\"; return order.Code;"));
        Assert.Equal("210", W("order.Total += 10; return order.Total;"));
        Assert.Equal("400", W("order.Total *= 2; return order.Total;"));
    }

    [Fact]
    public void CompoundAssignEvaluatesReceiverOnce()
        => Assert.Equal("3", W("order.Count += 1; return order.Count;"));

    [Fact]
    public void WriteNullableProperty()
    {
        Assert.Equal("5", W("order.Priority = 5; return order.Priority;"));
        Assert.Equal("null", W("order.Priority = 5; order.Priority = null; return order.Priority;"));
    }

    [Fact]
    public void WritingReadonlyFieldThrows()
        => Assert.Contains("没有可写成员", TestEngine.Fails("order.ReadonlyField = 1; return 1;", TestEngine.SampleOrder()).Message);

    [Fact]
    public void WritingReadOnlyPropertyThrows()
        => Assert.Contains("没有可写成员", TestEngine.Fails("order.Kind = \"x\"; return 1;", TestEngine.SampleOrder()).Message);

    [Fact]
    public void WritingUnknownMemberThrows()
        => Assert.Contains("Nope", TestEngine.Fails("order.Nope = 1; return 1;", TestEngine.SampleOrder()).Message);

    [Fact]
    public void PropertyTypeMismatchThrows()
        => Assert.Contains("无法转换", TestEngine.Fails("order.Count = \"abc\"; return 1;", TestEngine.SampleOrder()).Message);

    [Fact]
    public void IntPropertyRejectsFractionalValue()
        => Assert.Contains("无法转换", TestEngine.Fails("order.Count = 1.5; return 1;", TestEngine.SampleOrder()).Message);

    [Fact]
    public void IntPropertyRejectsOutOfRangeValue()
        => Assert.Contains("超出", TestEngine.Fails("order.Count = 99999999999; return 1;", TestEngine.SampleOrder()).Message);

    // ---------------------------------------------------------------- 实例方法

    [Fact]
    public void CallWithNoArgs()
        => Assert.Equal("X-9:200", W("return order.Describe();"));

    [Fact]
    public void CallInheritedMethod()
        => Assert.Equal("entity.Trace", W("return order.Trace();"));

    [Fact]
    public void CallReturningNull()
        => Assert.Equal("null", W("return order.MaybeNull();"));

    [Fact]
    public void CallVoidMethodReturnsNull()
        => Assert.Equal("null", W("return order.Bump();"));

    [Fact]
    public void VoidMethodSideEffectIsVisible()
        => Assert.Equal("3", W("order.Bump(); return order.Count;"));

    [Fact]
    public void CallAsBareStatement()
        => Assert.Equal("4", W("order.Bump(); order.Bump(); return order.Count;"));

    [Fact]
    public void CallInsideExpression()
        => Assert.Equal("40.0", W("return order.Discount(0.1) * 2;"));

    [Fact]
    public void CallResultCanBeChained()
        => Assert.Equal("X-9", W("return order.Describe().Substring(0, 3);"));

    // ---------------------------------------------------------------- 重载解析

    [Fact]
    public void OverloadByArgumentCount()
    {
        Assert.Equal("20.0", W("return order.Discount(0.1);"));
        Assert.Equal("15.0", W("return order.Discount(0.1, 5);"));
    }

    [Fact]
    public void IntegerArgumentPromotesToDecimalParameter()
        => Assert.Equal("200", W("return order.Discount(1);"));

    [Fact]
    public void OptionalParametersAreFilled()
    {
        Assert.Equal("x-!", W("return order.Label(\"x\");"));
        Assert.Equal("x+!", W("return order.Label(\"x\", \"+\");"));
        Assert.Equal("x+?", W("return order.Label(\"x\", \"+\", \"?\");"));
    }

    [Fact]
    public void OptionalParameterOnVoidMethod()
    {
        Assert.Equal("3", W("order.Bump(); return order.Count;"));
        Assert.Equal("12", W("order.Bump(10); return order.Count;"));
    }

    [Fact]
    public void ParamsArrayIsPacked()
    {
        Assert.Equal("a-b-c", W("return order.Join(\"-\", \"a\", \"b\", \"c\");"));
        Assert.Equal("a", W("return order.Join(\"-\", \"a\");"));
        Assert.Equal("", W("return order.Join(\"-\");"));            // params 收到空数组
        Assert.Equal("6", W("return order.SumAll(1, 2, 3);"));
        Assert.Equal("0", W("return order.SumAll();"));
    }

    [Fact]
    public void AmbiguousOverloadIsReported()
    {
        var ex = TestEngine.Fails("return order.Ambiguous(1);", TestEngine.SampleOrder());
        Assert.Contains("歧义", ex.Message);
        Assert.Contains("Ambiguous", ex.Message);
    }

    [Fact]
    public void GenericMethodIsNotResolved()
        => Assert.Contains("Echo", TestEngine.Fails("return order.Echo(1);", TestEngine.SampleOrder()).Message);

    [Fact]
    public void NoMatchingOverloadMentionsArgumentTypes()
    {
        var ex = TestEngine.Fails("return order.Discount(\"x\");", TestEngine.SampleOrder());
        Assert.Contains("Discount", ex.Message);
        Assert.Contains("string", ex.Message);
    }

    [Fact]
    public void WrongArgumentCountIsReported()
        => Assert.Contains("Describe", TestEngine.Fails("return order.Describe(1, 2, 3);", TestEngine.SampleOrder()).Message);

    [Fact]
    public void UnknownMethodIsReported()
        => Assert.Contains("没有方法", TestEngine.Fails("return order.Nope();", TestEngine.SampleOrder()).Message);

    // ---------------------------------------------------------------- 静态类型

    [Fact]
    public void StaticMethod()
    {
        Assert.Equal("2.34", TestEngine.Eval("return Math.Round(2.345, 2);"));   // Round(decimal,int) 银行家舍入
        Assert.Equal("5", TestEngine.Eval("return Math.Abs(-5);"));
        Assert.Equal("9", TestEngine.Eval("return Math.Max(9, 3);"));
        Assert.Equal("3", TestEngine.Eval("return Math.Min(9, 3);"));
    }

    [Fact]
    public void StaticMethodWithStringArg()
    {
        Assert.Equal("0.0925", TestEngine.Eval("return Tax.For(\"CA\");"));
        Assert.Equal("0.08", TestEngine.Eval("return Tax.For(\"TX\");"));
    }

    [Fact]
    public void StaticMethodWithTwoDecimals()
        => Assert.Equal("108.00", TestEngine.Eval("return Tax.Apply(100, 0.08);"));

    [Fact]
    public void StaticConstField()
        => Assert.Equal("0.08", TestEngine.Eval("return Tax.Rate;"));

    [Fact]
    public void StaticMutableFieldReadAndWrite()
    {
        var before = Tax.Region;
        try
        {
            Assert.Equal("CN", TestEngine.Eval("Tax.Region = \"CN\"; return Tax.Region;"));
        }
        finally
        {
            Tax.Region = before;
        }
    }

    [Fact]
    public void StaticReadonlyFieldCannotBeWritten()
        => Assert.Contains("没有可写成员", TestEngine.Fails("Tax.Fixed = \"x\"; return 1;").Message);

    [Fact]
    public void UnregisteredTypeIsJustAnExternalVariable()
    {
        // 没注册的类型名不会被解析成类型，而是当成外部变量（值为 null）
        Assert.Contains("null", TestEngine.Fails("return Console.WriteLine;").Message);
    }

    [Fact]
    public void TypeCannotBeCalledDirectly()
        => Assert.Contains(TestEngine.FailsToCompile("return Math(1);"), d => d.Message.Contains("是类型"));

    [Fact]
    public void RegisteredTypeAlias()
    {
        var engine = new ScriptEngine().RegisterType(typeof(Tax), "税");
        Assert.Equal("0.08", engine.Compile("return 税.Rate;").Run().ToDisplayString());
    }

    // ---------------------------------------------------------------- 枚举

    [Fact]
    public void EnumReadsAsName()
        => Assert.Equal("Paid", W("return order.State;"));

    [Fact]
    public void EnumComparesAgainstString()
        => Assert.Equal("true", W("return order.State == \"Paid\";"));

    [Fact]
    public void EnumWriteFromName()
        => Assert.Equal("Shipped", W("order.State = \"Shipped\"; return order.State;"));

    [Fact]
    public void EnumWriteFromNameIsCaseInsensitive()
        => Assert.Equal("Shipped", W("order.State = \"shipped\"; return order.State;"));

    [Fact]
    public void EnumWriteFromNumber()
        => Assert.Equal("Shipped", W("order.State = 2; return order.State;"));

    [Fact]
    public void EnumMemberOnRegisteredEnumType()
        => Assert.Equal("Paid", TestEngine.Eval("return OrderState.Paid;"));

    [Fact]
    public void EnumAsMethodArgument()
    {
        Assert.Equal("Paid", W("return order.Next(\"New\");"));
        Assert.Equal("Shipped", W("return order.Next(OrderState.Paid);"));
        Assert.Equal("Shipped", W("return order.Next(order.State);"));
    }

    [Fact]
    public void InvalidEnumNameThrows()
        => Assert.Contains("不是 OrderState 的有效值",
            TestEngine.Fails("order.State = \"Nope\"; return 1;", TestEngine.SampleOrder()).Message);

    // ---------------------------------------------------------------- 字符串（宿主 System.String）

    [Theory]
    [InlineData("return \"hello\".Length;", "5")]
    [InlineData("return \"hello\".StartsWith(\"he\");", "true")]
    [InlineData("return \"hello\".EndsWith(\"lo\");", "true")]
    [InlineData("return \"hello\".Contains(\"ell\");", "true")]
    [InlineData("return \"hello\".Substring(1, 3);", "ell")]
    [InlineData("return \"hello\".IndexOf(\"l\");", "2")]
    [InlineData("return \"Hello\".ToUpperInvariant();", "HELLO")]
    [InlineData("return \"Hello\".ToLowerInvariant();", "hello")]
    [InlineData("return \"  x  \".Trim();", "x")]
    [InlineData("return \"a-b\".Replace(\"-\", \"+\");", "a+b")]
    [InlineData("return \"ab\".PadLeft(4);", "  ab")]
    public void StringMembers(string src, string expected) => Assert.Equal(expected, TestEngine.Eval(src));

    [Fact]
    public void StringStaticsViaRegisteredAlias()
    {
        Assert.Equal("true", TestEngine.Eval("return Str.IsNullOrEmpty(\"\");"));
        Assert.Equal("false", TestEngine.Eval("return Str.IsNullOrEmpty(\"a\");"));
    }

    [Fact]
    public void MethodReturningArrayIsUsable()
    {
        Assert.Equal("2", TestEngine.Eval("return \"a,b\".Split(\",\").Length;"));
        Assert.Equal("ab", TestEngine.Eval("let s = \"\"; foreach (p in \"a,b\".Split(\",\")) { s += p; } return s;"));
    }

    // ---------------------------------------------------------------- 值类型接收者

    [Fact]
    public void MethodOnDecimalValue()
        => Assert.Equal("200", W("return order.Total.ToString();"));

    [Fact]
    public void MethodOnIntegerValue()
        => Assert.Equal("7", TestEngine.Eval("let n = 7; return n.ToString();"));

    [Fact]
    public void MethodOnNumberLiteral()
        => Assert.Equal("12", TestEngine.Eval("return 12.ToString();"));

    [Fact]
    public void MethodOnBoolValue()
        => Assert.Equal("True", TestEngine.Eval("return true.ToString();"));

    [Fact]
    public void StructReturnedFromHostIsUsable()
    {
        var r = TestEngine.Eval("return Guid.NewGuid().ToString().Length;");
        Assert.Equal("36", r);
    }

    // ---------------------------------------------------------------- 索引器

    [Fact]
    public void HostListIndexer()
        => Assert.Equal("blue", W("return order.Tags[1];"));

    [Fact]
    public void HostListIndexerWrite()
        => Assert.Equal("green", W("order.Tags[0] = \"green\"; return order.Tags[0];"));

    [Fact]
    public void HostDictionaryIndexer()
    {
        var order = TestEngine.SampleOrder();
        order.Meta["k"] = 3;
        Assert.Equal("3", W("return order.Meta[\"k\"];", order));
    }

    [Fact]
    public void HostDictionaryIndexerWrite()
        => Assert.Equal("9", W("order.Meta[\"n\"] = 9; return order.Meta[\"n\"];"));

    [Fact]
    public void CustomIndexer()
    {
        Assert.Equal("a", V("return m[0];", ("m", new Matrix())));
        Assert.Equal("z", V("m[0] = \"z\"; return m[0];", ("m", new Matrix())));
    }

    [Fact]
    public void HostListCountAndMethods()
    {
        Assert.Equal("2", W("return order.Tags.Count;"));
        Assert.Equal("3", W("order.Tags.Add(\"c\"); return order.Tags.Count;"));
        Assert.Equal("true", W("return order.Tags.Contains(\"red\");"));
    }

    [Fact]
    public void TypeWithoutIndexerThrows()
        => Assert.Contains("索引器", Assert.Throws<ScriptRuntimeException>(
            () => TestEngine.RunVars("return x[0];", ("x", new NoIndexer()))).Message);

    // ---------------------------------------------------------------- 宿主异常

    [Fact]
    public void HostExceptionIsWrappedWithPosition()
    {
        var ex = Assert.Throws<ScriptRuntimeException>(() =>
            TestEngine.New().Compile("let a = 1;\norder.Boom();\n")
                .Run(new Dictionary<string, object?> { ["order"] = TestEngine.SampleOrder() }));

        Assert.Equal(2, ex.Line);
        Assert.Contains("host boom", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    // ---------------------------------------------------------------- 内联缓存

    [Fact]
    public void SameSiteHandlesDifferentReceiverTypes()
    {
        // 同一个成员访问点先后遇到两种运行期类型，缓存必须重新解析
        var script = TestEngine.New().Compile("let s = \"\"; foreach (o in items) { s += o.Kind + \"|\"; } return s;");
        var items = new List<Entity> { new Entity(), TestEngine.SampleOrder(), new Entity() };
        var r = script.Run(new Dictionary<string, object?> { ["items"] = items });
        Assert.Equal("entity|order|entity|", r.ToDisplayString());
    }

    [Fact]
    public void SameCallSiteHandlesDifferentArgumentKinds()
    {
        // 实参类型不同可能选中不同重载，所以实参签名也是缓存键的一部分
        var script = TestEngine.New().Compile("""
            let s = "";
            foreach (r in rates) { s += order.Discount(r) + "|"; }
            return s;
            """);
        var r = script.Run(new Dictionary<string, object?>
        {
            ["order"] = TestEngine.SampleOrder(),
            ["rates"] = new List<Value> { Value.Dec(0.1m), Value.Int(1), Value.Dec(0.5m) },
        });
        Assert.Equal("20.0|200|100.0|", r.ToDisplayString());
    }

    [Fact]
    public void RepeatedCallsAreStable()
    {
        var script = TestEngine.New().Compile("return order.Discount(0.1);");
        var vars = new Dictionary<string, object?> { ["order"] = TestEngine.SampleOrder() };
        for (int i = 0; i < 100; i++)
            Assert.Equal("20.0", script.Run(vars).ToDisplayString());
    }

    // ---------------------------------------------------------------- 可重入

    [Fact]
    public void HostMethodMayRunAnotherScript()
    {
        // 宿主方法内部又执行脚本，VM 的线程本地缓冲必须能退化处理
        Assert.Equal("42", V("return re.Double(21);", ("re", new Reentrant())));
    }

    [Fact]
    public void NestedScriptExecutionInsideLoop()
        => Assert.Equal("[0, 2, 4]", V("""
            let a = [];
            for (let i = 0; i < 3; i += 1) { a.Add(re.Double(i)); }
            return a;
            """, ("re", new Reentrant())));

    // ---------------------------------------------------------------- 综合场景

    [Fact]
    public void RealisticPricingRule()
    {
        const string src = """
            // 会员打折，满三件再打九折，最后按地区加税
            let rate = order.Customer.IsVip ? 0.8 : 1.0;
            let sub = order.Total * rate;
            if (order.Count >= 3) {
                sub = sub * 0.9;
            }
            return Math.Round(sub * (1 + Tax.For(order.Ship.Region)), 2);
            """;
        var order = TestEngine.SampleOrder();
        order.Count = 3;
        Assert.Equal("157.32", W(src, order));
    }

    [Fact]
    public void RealisticValidationRule()
    {
        const string src = """
            let errors = [];
            if (order.Code == null or order.Code.Length < 3) { errors.Add("code"); }
            if (order.Total <= 0) { errors.Add("total"); }
            if (order.Count <= 0) { errors.Add("count"); }
            foreach (t in order.Tags) {
                if (t.Length > 10) { errors.Add("tag:" + t); }
            }
            return errors;
            """;
        Assert.Equal("[]", W(src));

        var bad = TestEngine.SampleOrder();
        bad.Code = "ab";
        bad.Total = 0m;
        Assert.Equal("[\"code\", \"total\"]", W(src, bad));
    }

    [Fact]
    public void RealisticTagBasedRouting()
        => Assert.Equal("express", W("""
            foreach (t in order.Tags) {
                if (t == "red") { return "express"; }
            }
            return "standard";
            """));
}

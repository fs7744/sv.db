using SV.Script.Runtime;

namespace SV.Script.Tests;

public class ValueTest
{
    // ---------------------------------------------------------------- 构造与 Kind

    [Fact]
    public void Factories()
    {
        Assert.Equal(ValueKind.Null, Value.Null.Kind);
        Assert.Equal(ValueKind.Bool, Value.True.Kind);
        Assert.Equal(ValueKind.Int, Value.Zero.Kind);
        Assert.Equal(ValueKind.Int, Value.Int(1).Kind);
        Assert.Equal(ValueKind.Dec, Value.Dec(1m).Kind);
        Assert.Equal(ValueKind.Str, Value.Str("x").Kind);
        Assert.Equal(ValueKind.Array, Value.Arr(new ScriptArray()).Kind);
        Assert.Equal(ValueKind.Map, Value.Map(new ScriptMap()).Kind);
        Assert.Equal(ValueKind.Type, Value.TypeRef(typeof(int)).Kind);
        Assert.Equal(ValueKind.Object, Value.Obj(new object()).Kind);
    }

    [Fact]
    public void DefaultValueIsNull()
    {
        Assert.True(default(Value).IsNull);
        Assert.True(Value.Null.IsNull);
        Assert.False(Value.Zero.IsNull);
    }

    [Fact]
    public void ObjOfNullBecomesNull()
        => Assert.True(Value.Obj(null).IsNull);

    [Fact]
    public void IsNumberCoversIntAndDecOnly()
    {
        Assert.True(Value.Int(1).IsNumber);
        Assert.True(Value.Dec(1m).IsNumber);
        Assert.False(Value.True.IsNumber);
        Assert.False(Value.Str("1").IsNumber);
        Assert.False(Value.Null.IsNumber);
    }

    [Fact]
    public void Accessors()
    {
        Assert.True(Value.True.AsBool);
        Assert.False(Value.False.AsBool);
        Assert.Equal(5L, Value.Int(5).AsInt);
        Assert.Equal(2.5m, Value.Dec(2.5m).AsDec);
        Assert.Equal(5m, Value.Int(5).AsDec);            // Int 也能按 decimal 读
        Assert.Equal("x", Value.Str("x").AsStr);
        Assert.Equal(typeof(int), Value.TypeRef(typeof(int)).AsType);
    }

    // ---------------------------------------------------------------- 脚本语义相等

    [Fact]
    public void NumbersCompareAcrossRepresentations()
    {
        Assert.True(Value.Int(1).Equals(Value.Dec(1m)));
        Assert.True(Value.Dec(1.0m).Equals(Value.Int(1)));
        Assert.True(Value.Dec(1.00m).Equals(Value.Dec(1.0m)));   // decimal 相等忽略标度
        Assert.False(Value.Int(1).Equals(Value.Int(2)));
    }

    [Fact]
    public void DifferentKindsAreNotEqual()
    {
        Assert.False(Value.Int(1).Equals(Value.Str("1")));
        Assert.False(Value.Int(1).Equals(Value.True));
        Assert.False(Value.Null.Equals(Value.Zero));
        Assert.False(Value.True.Equals(Value.Int(1)));
    }

    [Fact]
    public void NullEqualsNull() => Assert.True(Value.Null.Equals(Value.Null));

    [Fact]
    public void StringsCompareByOrdinalContent()
    {
        Assert.True(Value.Str("ab").Equals(Value.Str("ab")));
        Assert.False(Value.Str("ab").Equals(Value.Str("AB")));
    }

    [Fact]
    public void ReferenceTypesCompareByIdentity()
    {
        var a = new ScriptArray();
        Assert.True(Value.Arr(a).Equals(Value.Arr(a)));
        Assert.False(Value.Arr(a).Equals(Value.Arr(new ScriptArray())));
    }

    [Fact]
    public void HashCodeAgreesWithEquals()
    {
        Assert.Equal(Value.Int(1).GetHashCode(), Value.Dec(1m).GetHashCode());
        Assert.Equal(Value.Int(1).GetHashCode(), Value.Dec(1.000m).GetHashCode());
        Assert.Equal(Value.Str("x").GetHashCode(), Value.Str("x").GetHashCode());
    }

    [Fact]
    public void EqualsObjectOverload()
    {
        Assert.True(Value.Int(1).Equals((object)Value.Int(1)));
        Assert.False(Value.Int(1).Equals((object?)null));
        Assert.False(Value.Int(1).Equals("not a value"));
    }

    // ---------------------------------------------------------------- 精确比较器（常量池用）

    [Fact]
    public void ExactComparerDistinguishesKinds()
    {
        var c = Value.ExactComparer;
        Assert.False(c.Equals(Value.Int(1), Value.Dec(1m)));
        Assert.True(c.Equals(Value.Int(1), Value.Int(1)));
        Assert.True(c.Equals(Value.Dec(1m), Value.Dec(1m)));
    }

    [Fact]
    public void ExactComparerDistinguishesDecimalScale()
    {
        var c = Value.ExactComparer;
        Assert.False(c.Equals(Value.Dec(1.0m), Value.Dec(1.00m)));
        Assert.True(c.Equals(Value.Dec(1.0m), Value.Dec(1.0m)));
    }

    [Fact]
    public void ExactComparerHandlesNullAndStrings()
    {
        var c = Value.ExactComparer;
        Assert.True(c.Equals(Value.Null, Value.Null));
        Assert.True(c.Equals(Value.Str("a"), Value.Str("a")));
        Assert.False(c.Equals(Value.Str("a"), Value.Str("b")));
    }

    [Fact]
    public void ExactComparerHashIsStable()
    {
        var c = Value.ExactComparer;
        Assert.Equal(c.GetHashCode(Value.Int(7)), c.GetHashCode(Value.Int(7)));
        Assert.Equal(c.GetHashCode(Value.Str("s")), c.GetHashCode(Value.Str("s")));
    }

    // ---------------------------------------------------------------- 显示与类型名

    [Theory]
    [InlineData(null, "null")]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void DisplayOfSimpleValues(bool? b, string expected)
        => Assert.Equal(expected, (b is null ? Value.Null : Value.Bool(b.Value)).ToDisplayString());

    [Fact]
    public void DisplayOfNumbersUsesInvariantCulture()
    {
        Assert.Equal("1", Value.Int(1).ToDisplayString());
        Assert.Equal("-1", Value.Int(-1).ToDisplayString());
        Assert.Equal("1.50", Value.Dec(1.50m).ToDisplayString());
        Assert.Equal("0.001", Value.Dec(0.001m).ToDisplayString());
    }

    [Fact]
    public void DisplayOfCollections()
    {
        var a = new ScriptArray();
        a.Add(Value.Int(1));
        a.Add(Value.Str("s"));
        Assert.Equal("[1, \"s\"]", Value.Arr(a).ToDisplayString());

        var m = new ScriptMap();
        m["k"] = Value.Int(1);
        Assert.Equal("{k: 1}", Value.Map(m).ToDisplayString());
    }

    [Fact]
    public void ToStringMatchesToDisplayString()
        => Assert.Equal(Value.Int(1).ToDisplayString(), Value.Int(1).ToString());

    [Theory]
    [InlineData(ValueKind.Null, "null")]
    [InlineData(ValueKind.Bool, "bool")]
    [InlineData(ValueKind.Int, "int")]
    [InlineData(ValueKind.Dec, "decimal")]
    [InlineData(ValueKind.Str, "string")]
    [InlineData(ValueKind.Array, "array")]
    [InlineData(ValueKind.Map, "map")]
    public void TypeNames(ValueKind kind, string expected)
    {
        var v = kind switch
        {
            ValueKind.Null => Value.Null,
            ValueKind.Bool => Value.True,
            ValueKind.Int => Value.Int(1),
            ValueKind.Dec => Value.Dec(1m),
            ValueKind.Str => Value.Str("s"),
            ValueKind.Array => Value.Arr(new ScriptArray()),
            _ => Value.Map(new ScriptMap()),
        };
        Assert.Equal(expected, v.TypeName);
    }

    [Fact]
    public void TypeNameOfHostObjectUsesClrTypeName()
        => Assert.Equal("Order", Value.Obj(new Order()).TypeName);

    [Fact]
    public void ToClrObjectUnwraps()
    {
        Assert.Null(Value.Null.ToClrObject());
        Assert.Equal(true, Value.True.ToClrObject());
        Assert.Equal(1L, Value.Int(1).ToClrObject());
        Assert.Equal(1.5m, Value.Dec(1.5m).ToClrObject());
        Assert.Equal("s", Value.Str("s").ToClrObject());
    }

    // ---------------------------------------------------------------- Marshaller: CLR -> Value

    [Fact]
    public void FromClrCoversCommonTypes()
    {
        Assert.True(Marshaller.FromClr(null).IsNull);
        Assert.Equal(ValueKind.Bool, Marshaller.FromClr(true).Kind);
        Assert.Equal(ValueKind.Str, Marshaller.FromClr("s").Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr((sbyte)1).Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr((byte)1).Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr((short)1).Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr(1).Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr(1L).Kind);
        Assert.Equal(ValueKind.Int, Marshaller.FromClr(1u).Kind);
        Assert.Equal(ValueKind.Dec, Marshaller.FromClr(1.5f).Kind);
        Assert.Equal(ValueKind.Dec, Marshaller.FromClr(1.5d).Kind);
        Assert.Equal(ValueKind.Dec, Marshaller.FromClr(1.5m).Kind);
        Assert.Equal(ValueKind.Str, Marshaller.FromClr('c').Kind);
        Assert.Equal(ValueKind.Array, Marshaller.FromClr(new ScriptArray()).Kind);
        Assert.Equal(ValueKind.Map, Marshaller.FromClr(new ScriptMap()).Kind);
        Assert.Equal(ValueKind.Type, Marshaller.FromClr(typeof(int)).Kind);
        Assert.Equal(ValueKind.Object, Marshaller.FromClr(new Order()).Kind);
    }

    [Fact]
    public void FromClrPassesValueThrough()
        => Assert.Equal(ValueKind.Int, Marshaller.FromClr(Value.Int(3)).Kind);

    [Fact]
    public void FromClrMapsEnumToItsName()
        => Assert.Equal("Paid", Marshaller.FromClr(OrderState.Paid).ToDisplayString());

    [Fact]
    public void FromClrKeepsSmallULongAsInt()
        => Assert.Equal(ValueKind.Int, Marshaller.FromClr(5UL).Kind);

    [Fact]
    public void FromClrPromotesLargeULongToDecimal()
        => Assert.Equal(ValueKind.Dec, Marshaller.FromClr(ulong.MaxValue).Kind);

    [Fact]
    public void FromClrCharBecomesSingleCharString()
        => Assert.Equal("c", Marshaller.FromClr('c').ToDisplayString());

    // ---------------------------------------------------------------- Marshaller: Value -> CLR

    [Fact]
    public void ToClrNumericConversions()
    {
        Assert.Equal(1L, Marshaller.ToI64(Value.Int(1)));
        Assert.Equal(1, Marshaller.ToI32(Value.Int(1)));
        Assert.Equal((short)1, Marshaller.ToI16(Value.Int(1)));
        Assert.Equal((sbyte)1, Marshaller.ToI8(Value.Int(1)));
        Assert.Equal((byte)1, Marshaller.ToU8(Value.Int(1)));
        Assert.Equal((ushort)1, Marshaller.ToU16(Value.Int(1)));
        Assert.Equal(1u, Marshaller.ToU32(Value.Int(1)));
        Assert.Equal(1UL, Marshaller.ToU64(Value.Int(1)));
        Assert.Equal(1.5m, Marshaller.ToDec(Value.Dec(1.5m)));
        Assert.Equal(1.5d, Marshaller.ToF64(Value.Dec(1.5m)));
        Assert.Equal(1.5f, Marshaller.ToF32(Value.Dec(1.5m)));
    }

    [Fact]
    public void ToI64AcceptsWholeDecimals()
    {
        Assert.Equal(2L, Marshaller.ToI64(Value.Dec(2m)));
        Assert.Equal(2L, Marshaller.ToI64(Value.Dec(2.000m)));
    }

    [Fact]
    public void ToI64RejectsFractionalDecimals()
        => Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToI64(Value.Dec(1.5m)));

    [Fact]
    public void RangeChecksAreEnforced()
    {
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToI32(Value.Int(long.MaxValue)));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToU8(Value.Int(300)));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToU64(Value.Int(-1)));
    }

    [Fact]
    public void ToBoolIsStrict()
    {
        Assert.True(Marshaller.ToBool(Value.True));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToBool(Value.Int(1)));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToBool(Value.Null));
    }

    [Fact]
    public void ToStrIsLenient()
    {
        Assert.Null(Marshaller.ToStr(Value.Null));
        Assert.Equal("s", Marshaller.ToStr(Value.Str("s")));
        Assert.Equal("1", Marshaller.ToStr(Value.Int(1)));
        Assert.Equal("true", Marshaller.ToStr(Value.True));
    }

    [Fact]
    public void ToCharRequiresSingleCharString()
    {
        Assert.Equal('a', Marshaller.ToChar(Value.Str("a")));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToChar(Value.Str("ab")));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToChar(Value.Int(1)));
    }

    [Fact]
    public void ToEnumAcceptsNameAndNumber()
    {
        Assert.Equal(OrderState.Paid, Marshaller.ToEnum(Value.Str("Paid"), typeof(OrderState)));
        Assert.Equal(OrderState.Paid, Marshaller.ToEnum(Value.Str("paid"), typeof(OrderState)));
        Assert.Equal(OrderState.Paid, Marshaller.ToEnum(Value.Int(1), typeof(OrderState)));
    }

    [Fact]
    public void ToEnumRejectsUnknownName()
        => Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToEnum(Value.Str("Nope"), typeof(OrderState)));

    [Fact]
    public void ToClrHandlesNullableTargets()
    {
        Assert.Null(Marshaller.ToClr(Value.Null, typeof(int?)));
        Assert.Equal(1, Marshaller.ToClr(Value.Int(1), typeof(int?)));
    }

    [Fact]
    public void ToClrRejectsNullForNonNullableValueTypes()
        => Assert.Throws<ScriptRuntimeException>(() => Marshaller.ToClr(Value.Null, typeof(int)));

    [Fact]
    public void ToClrAllowsNullForReferenceTypes()
        => Assert.Null(Marshaller.ToClr(Value.Null, typeof(string)));

    [Fact]
    public void ToClrPassesValueThrough()
        => Assert.Equal(Value.Int(1), Marshaller.ToClr(Value.Int(1), typeof(Value)));

    [Fact]
    public void ToClrPassesHostObjectThrough()
    {
        var o = new Order();
        Assert.Same(o, Marshaller.ToClr(Value.Obj(o), typeof(Order)));
        Assert.Same(o, Marshaller.ToClr(Value.Obj(o), typeof(Entity)));
    }

    [Fact]
    public void ToClrConvertsScriptArrayToClrArray()
    {
        var a = new ScriptArray();
        a.Add(Value.Int(1));
        a.Add(Value.Int(2));
        Assert.Equal(new[] { 1, 2 }, (int[])Marshaller.ToClr(Value.Arr(a), typeof(int[]))!);
    }

    [Fact]
    public void ToObjectTargetBoxesScalars()
    {
        Assert.Equal(1L, Marshaller.ToClr(Value.Int(1), typeof(object)));
        Assert.Equal("s", Marshaller.ToClr(Value.Str("s"), typeof(object)));
    }

    // ---------------------------------------------------------------- 迭代

    [Fact]
    public void EnumerateArray()
    {
        var a = new ScriptArray();
        a.Add(Value.Int(1));
        a.Add(Value.Int(2));

        var e = Marshaller.Enumerate(Value.Arr(a));
        var got = new List<long>();
        while (e.MoveNext()) got.Add(e.Current.AsInt);
        Assert.Equal([1L, 2L], got);
    }

    [Fact]
    public void EnumerateMapYieldsKeys()
    {
        var m = new ScriptMap();
        m["a"] = Value.Int(1);
        m["b"] = Value.Int(2);

        var e = Marshaller.Enumerate(Value.Map(m));
        var got = new List<string>();
        while (e.MoveNext()) got.Add(e.Current.AsStr);
        Assert.Equal(["a", "b"], got);
    }

    [Fact]
    public void EnumerateStringYieldsChars()
    {
        var e = Marshaller.Enumerate(Value.Str("ab"));
        var got = new List<string>();
        while (e.MoveNext()) got.Add(e.Current.AsStr);
        Assert.Equal(["a", "b"], got);
    }

    [Fact]
    public void EnumerateHostEnumerable()
    {
        var e = Marshaller.Enumerate(Value.Obj(new List<int> { 1, 2 }));
        var got = new List<long>();
        while (e.MoveNext()) got.Add(e.Current.AsInt);
        Assert.Equal([1L, 2L], got);
    }

    [Fact]
    public void EnumerateRejectsNullAndNonIterable()
    {
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.Enumerate(Value.Null));
        Assert.Throws<ScriptRuntimeException>(() => Marshaller.Enumerate(Value.Int(1)));
    }

    // ---------------------------------------------------------------- ScriptArray / ScriptMap 直接使用

    [Fact]
    public void ScriptArrayBasics()
    {
        var a = new ScriptArray();
        Assert.Equal(0, a.Count);

        a.Add(Value.Int(1));
        a.Add(Value.Int(2));
        Assert.Equal(2, a.Count);
        Assert.Equal(1L, a[0].AsInt);

        a[0] = Value.Int(9);
        Assert.Equal(9L, a[0].AsInt);

        a.Insert(0, Value.Int(0));
        Assert.Equal(0L, a[0].AsInt);

        a.RemoveAt(0);
        Assert.Equal(9L, a[0].AsInt);

        Assert.True(a.Contains(Value.Int(2)));
        Assert.Equal(1, a.IndexOf(Value.Int(2)));

        a.Clear();
        Assert.Equal(0, a.Count);
    }

    [Fact]
    public void ScriptArrayBoundsAreChecked()
    {
        var a = new ScriptArray();
        Assert.Throws<ScriptRuntimeException>(() => a[0]);
        Assert.Throws<ScriptRuntimeException>(() => a[0] = Value.Null);
        Assert.Throws<ScriptRuntimeException>(() => a.RemoveAt(0));
    }

    [Fact]
    public void ScriptArrayConstructorFromSequence()
        => Assert.Equal(2, new ScriptArray([Value.Int(1), Value.Int(2)]).Count);

    [Fact]
    public void ScriptMapBasics()
    {
        var m = new ScriptMap();
        Assert.Equal(0, m.Count);
        Assert.True(m["missing"].IsNull);        // 缺键读作 null，不抛异常

        m["a"] = Value.Int(1);
        Assert.Equal(1, m.Count);
        Assert.True(m.ContainsKey("a"));
        Assert.False(m.ContainsKey("A"));        // 键区分大小写

        Assert.Equal("[\"a\"]", m.Keys().ToDisplayString());
        Assert.Equal("[1]", m.Values().ToDisplayString());

        Assert.True(m.Remove("a"));
        Assert.False(m.Remove("a"));

        m["b"] = Value.Int(2);
        m.Clear();
        Assert.Equal(0, m.Count);
    }

    [Fact]
    public void ScriptMapPreservesInsertionOrder()
    {
        var m = new ScriptMap();
        m["z"] = Value.Int(1);
        m["a"] = Value.Int(2);
        Assert.Equal("[\"z\", \"a\"]", m.Keys().ToDisplayString());
    }
}

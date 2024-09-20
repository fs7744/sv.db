using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.Statements;
using System.Linq.Expressions;

namespace UT.Sloth
{
    [Table("QueryTest")]
    public class QueryTest
    {
        public int A { get; set; }
        public double B { get; set; }
        public bool T { get; set; }
        public bool? T2 { get; set; }
        public string Ts { get; set; }

        [Fact]
        public void SelectFields()
        {
            var a = From.Of<QueryTest>();
            a.Select(nameof(QueryTest.A), nameof(QueryTest.B));
            a.Select(i => i.A, i => i.B);
        }

        [Fact]
        public void Where()
        {
            AssertWhere<QueryTest, OperaterStatement>(i => i.A > 1, o =>
            {
                Assert.Equal(">", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A >= 12, o =>
            {
                Assert.Equal(">=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(12, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A <= 121, o =>
            {
                Assert.Equal("<=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(121, v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.B != 121, o =>
            {
                Assert.Equal("!=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("B", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(121, v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.A < 121.4, o =>
            {
                Assert.Equal("<", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(new decimal(121.4), v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A == 1, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => 1 == i.A, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Right);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Left);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.T, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("T", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.True(v.Value);
            });

            AssertWhere<QueryTest, UnaryOperaterStatement>(i => !i.T, oo =>
            {
                Assert.Equal("not", oo.Operater);
                var o = Assert.IsType<OperaterStatement>(oo.Right);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("T", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.True(v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.T2 == false, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("T2", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.False(v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts == "s", o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);
            });

            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" & !i.T, oo =>
            {
                Assert.Equal(Condition.And, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldValueStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" && !i.T, oo =>
            {
                Assert.Equal(Condition.And, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldValueStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" || !i.T, oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldValueStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" | !i.T, oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldValueStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldValueStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" && i.Ts == "s1" && (!i.T || i.T || i.T) && (!i.T || i.T || i.T), oo =>
            {
                Assert.Equal(Condition.And, oo.Condition);
            });
            var d = 4;
            var dd = new QueryTest();
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" | i.A == d | i.B != dd.B | i.Ts == dd.Ts, oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);

                var o2 = Assert.IsType<OperaterStatement>(oo.Right);
                Assert.Equal("=", o2.Operater);
                var o3 = Assert.IsType<NullValueStatement>(o2.Right);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" | i.A == d | i.B != dd.B | i.T == dd.T2.GetValueOrDefault(), oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);

                var o2 = Assert.IsType<OperaterStatement>(oo.Right);
                Assert.Equal("=", o2.Operater);
                var o3 = Assert.IsType<BooleanValueStatement>(o2.Right);
                Assert.False(o3.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" | i.A == d | i.B != dd.B | i.T == (3 == 4), oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);

                var o2 = Assert.IsType<OperaterStatement>(oo.Right);
                Assert.Equal("=", o2.Operater);
                var o3 = Assert.IsType<BooleanValueStatement>(o2.Right);
                Assert.False(o3.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts.PrefixLike("s"), oo =>
            {
                Assert.Equal("PrefixLike", oo.Operater);

                var o2 = Assert.IsType<StringValueStatement>(oo.Right);
                Assert.Equal("s", o2.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts.SuffixLike("s" + "sd"), oo =>
            {
                Assert.Equal("SuffixLike", oo.Operater);

                var o2 = Assert.IsType<StringValueStatement>(oo.Right);
                Assert.Equal("ssd", o2.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts.Like(true.ToString()), oo =>
            {
                Assert.Equal("Like", oo.Operater);

                var o2 = Assert.IsType<StringValueStatement>(oo.Right);
                Assert.Equal("True", o2.Value);
            });
            AssertWhere<QueryTest, InOperaterStatement>(i => i.Ts.In(true.ToString()), oo =>
            {
                Assert.Equal("in", oo.Operater);
                var o2 = Assert.IsType<StringArrayValueStatement>(oo.Right);
                Assert.Equal(1, o2.Value.Count);
                Assert.Equal("True", o2.Value.First());
            });
            AssertWhere<QueryTest, InOperaterStatement>(i => i.Ts.In(1 == 3), oo =>
            {
                Assert.Equal("in", oo.Operater);
                var o2 = Assert.IsType<BooleanArrayValueStatement>(oo.Right);
                Assert.Equal(1, o2.Value.Count);
                Assert.False(o2.Value.First());
            });
            AssertWhere<QueryTest, InOperaterStatement>(i => i.Ts.In(12), oo =>
            {
                Assert.Equal("in", oo.Operater);
                var o2 = Assert.IsType<NumberArrayValueStatement>(oo.Right);
                Assert.Equal(1, o2.Value.Count);
                Assert.Equal(12, o2.Value.First());
            });
            Assert.Throws<NotSupportedException>(() => AssertWhere<QueryTest, UnaryOperaterStatement>(i => i.T2.GetValueOrDefault(), o => { }));
        }

        public void AssertWhere<T, O>(Expression<Func<T, bool>> expr, Action<O> action)
        {
            var a = From.Of<T>();
            var s = a.Where(expr).Build();
            Assert.NotNull(s);
            Assert.NotNull(s.Where);
            Assert.NotNull(s.Where.Condition);
            var o = Assert.IsType<O>(s.Where.Condition);
            action(o);
        }
    }
}
﻿using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.Statements;
using System.Linq.Expressions;

namespace UT.Sloth
{
    [Table("QueryTest")]
    [Db("t")]
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
            var a = new ConnectionStringProviders(null, null, null).From<QueryTest>();
            a.Select("json", nameof(QueryTest.A), nameof(QueryTest.B));
            a.Select(nameof(QueryTest.A), nameof(QueryTest.B), "json");
            a.Select(i => i.A, i => i.B, i => i.A.JsonExtract("$.s"), i => i.B.JsonExtract("$.sdd", "d"));
            Assert.Equal("B,A,json,json,B,A,json(B,'$.sdd',d),json(A,'$.s'),B,A", From.ParseToQueryParams(a.Build(new SelectStatementOptions() { AllowNotFoundFields = true, AllowNonStrictCondition = true }))["fields"]);
        }

        [Fact]
        public void GroupByFuncFields()
        {
            var a = new ConnectionStringProviders(null, null, null).From<QueryTest>();
            a.Select(i => i.A.Count("ac"), i => i.A.Min("ac"), i => i.A.Max("ac"), i => i.A.Sum("ac"));
            a.Select("count", "min", "max", "sum");
            a.Select("count(a,ac)", "min(a,ac)", "max(a,ac)", "sum(a,ac)");
            a.Select("count(a)", "min(a)", "max(a)", "sum(a)");
            Assert.Equal("Sum(A,ac),Max(A,ac),Min(A,ac),Count(A,ac),sum,max,min,count,sum(a,ac),max(a,ac),min(a,ac),count(a,ac),sum(a),max(a),min(a),count(a)", From.ParseToQueryParams(a.Build(new SelectStatementOptions() { AllowNotFoundFields = true, AllowNonStrictCondition = true }))["fields"]);
        }

        [Fact]
        public void GroupByFields()
        {
            var a = new ConnectionStringProviders(null, null, null).From<QueryTest>();
            a.GroupBy("json", nameof(QueryTest.A), nameof(QueryTest.B));
            a.GroupBy(nameof(QueryTest.A), nameof(QueryTest.B), "json");
            a.GroupBy(i => i.A, i => i.B, i => i.A.JsonExtract("$.s"), i => i.B.JsonExtract("$.sdd", "d"));
            Assert.Equal("B,A,json,json,B,A,json(B,'$.sdd',d),json(A,'$.s'),B,A", From.ParseToQueryParams(a.Build(new SelectStatementOptions() { AllowNotFoundFields = true, AllowNonStrictCondition = true }))["GroupBy"]);
        }

        [Fact]
        public void SelectOrderByFields()
        {
            var a = new ConnectionStringProviders(null, null, null).From<QueryTest>();
            a.OrderBy("json", nameof(QueryTest.A), nameof(QueryTest.B));
            a.OrderBy(nameof(QueryTest.A), nameof(QueryTest.B), "json");
            a.OrderBy(i => i.A, i => i.B, i => i.A.JsonExtract("$.s"), i => i.B.JsonExtract("$.sdd", "d").Desc());
            Assert.Equal("B Asc,A Asc,json Asc,json Asc,B Asc,A Asc,json(B,'$.sdd',d) Desc,json(A,'$.s') Asc,B Asc,A Asc", From.ParseToQueryParams(a.Build(new SelectStatementOptions() { AllowNotFoundFields = true, AllowNonStrictCondition = true }))["orderby"]);
        }

        [Fact]
        public void Where()
        {
            AssertWhere<QueryTest, OperaterStatement>(i => i.A.JsonExtract("$.a") == 1, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<JsonFieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                Assert.Equal("$.a", f.Path);
                Assert.Empty(f.As);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(1, v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.A > 1, o =>
            {
                Assert.Equal(">", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A >= 12, o =>
            {
                Assert.Equal(">=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(12, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A <= 121, o =>
            {
                Assert.Equal("<=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(121, v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.B != 121, o =>
            {
                Assert.Equal("!=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("B", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(121, v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.A < 121.4, o =>
            {
                Assert.Equal("<", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(new decimal(121.4), v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.A == 1, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Right);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => 1 == i.A, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Right);
                Assert.Equal("A", f.Field);
                var v = Assert.IsType<NumberValueStatement>(o.Left);
                Assert.Equal(1, v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.T, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("T", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.True(v.Value);
            });

            AssertWhere<QueryTest, UnaryOperaterStatement>(i => !i.T, oo =>
            {
                Assert.Equal("not", oo.Operater);
                var o = Assert.IsType<OperaterStatement>(oo.Right);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("T", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.True(v.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.T2 == false, o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("T2", f.Field);
                var v = Assert.IsType<BooleanValueStatement>(o.Right);
                Assert.False(v.Value);
            });

            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts == "s", o =>
            {
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);
            });

            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" & !i.T, oo =>
            {
                Assert.Equal(Condition.And, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" && !i.T, oo =>
            {
                Assert.Equal(Condition.And, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" || !i.T, oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldStatement>(o3.Left);
                Assert.Equal("T", f2.Field);
                var v2 = Assert.IsType<BooleanValueStatement>(o3.Right);
                Assert.True(v2.Value);
            });
            AssertWhere<QueryTest, ConditionsStatement>(i => i.Ts == "s" | !i.T, oo =>
            {
                Assert.Equal(Condition.Or, oo.Condition);
                var o = Assert.IsType<OperaterStatement>(oo.Left);
                Assert.Equal("=", o.Operater);
                var f = Assert.IsType<FieldStatement>(o.Left);
                Assert.Equal("Ts", f.Field);
                var v = Assert.IsType<StringValueStatement>(o.Right);
                Assert.Equal("s", v.Value);

                var o2 = Assert.IsType<UnaryOperaterStatement>(oo.Right);
                Assert.Equal("not", o2.Operater);
                var o3 = Assert.IsType<OperaterStatement>(o2.Right);
                Assert.Equal("=", o.Operater);
                var f2 = Assert.IsType<FieldStatement>(o3.Left);
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
                Assert.Equal("is-null", o2.Operater);
                var o3 = Assert.IsType<NullValueStatement>(o2.Right);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts != null, o2 =>
            {
                Assert.Equal("not-null", o2.Operater);
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
                Assert.Equal("prefix-like", oo.Operater);

                var o2 = Assert.IsType<StringValueStatement>(oo.Right);
                Assert.Equal("s", o2.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts.SuffixLike("s" + "sd"), oo =>
            {
                Assert.Equal("suffix-like", oo.Operater);

                var o2 = Assert.IsType<StringValueStatement>(oo.Right);
                Assert.Equal("ssd", o2.Value);
            });
            AssertWhere<QueryTest, OperaterStatement>(i => i.Ts.Like(true.ToString()), oo =>
            {
                Assert.Equal("like", oo.Operater);

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

            AssertWhere<QueryTest, OperaterStatement>("3 = 4", oo =>
            {
                Assert.Equal("=", oo.Operater);
                var o2 = Assert.IsType<NumberValueStatement>(oo.Left);
                Assert.Equal(3, o2.Value);
                var o3 = Assert.IsType<NumberValueStatement>(oo.Right);
                Assert.Equal(4, o3.Value);
            });
        }

        public void AssertWhere<T, O>(Expression<Func<T, bool>> expr, Action<O> action)
        {
            var a = new ConnectionStringProviders(null, null, null).From<T>();
            var s = a.Where(expr).Build(new SelectStatementOptions() { AllowNotFoundFields = true });
            Assert.NotNull(s);
            Assert.NotNull(s.Where);
            Assert.NotNull(s.Where.Condition);
            var o = Assert.IsType<O>(s.Where.Condition);
            action(o);
        }

        public void AssertWhere<T, O>(string expr, Action<O> action)
        {
            var a = new ConnectionStringProviders(null, null, null).From<T>();
            var s = a.Where(expr).Build(new SelectStatementOptions() { AllowNotFoundFields = true, AllowNonStrictCondition = true });
            Assert.NotNull(s);
            Assert.NotNull(s.Where);
            Assert.NotNull(s.Where.Condition);
            var o = Assert.IsType<O>(s.Where.Condition);
            action(o);
        }
    }
}
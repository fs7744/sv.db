﻿using SV.Db.Sloth;
using SV.Db.Sloth.SqlParser;
using SV.Db.Sloth.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT.SqlParser
{
    public class SqlStatementParserTest
    {
        [Theory]
        [InlineData("(json(v,\"$.a\") = 2)", " json(v,'$.a') = 2 ", typeof(OperaterStatement))]
        [InlineData("json(yy,\"$.a[2]\", name) = NULL ", " json(yy,'$.a[2]',name) = null ", typeof(OperaterStatement))]
        [InlineData("1", "1", typeof(NumberValueStatement))]
        [InlineData("1.3", "1.3", typeof(NumberValueStatement))]
        [InlineData("-77.3", "-77.3", typeof(NumberValueStatement))]
        [InlineData("'sdd sd'", "sdd sd", typeof(StringValueStatement))]
        [InlineData("true", "tRue", typeof(BooleanValueStatement))]
        [InlineData("false", "false", typeof(BooleanValueStatement))]
        [InlineData("xx", "xx", typeof(FieldStatement))]
        [InlineData("v in (1)", " v in (1) ", typeof(InOperaterStatement))]
        [InlineData("v in (1,2,3,4)", " v in (1,2,3,4) ", typeof(InOperaterStatement))]
        [InlineData("v in ('1')", " v in ('1') ", typeof(InOperaterStatement))]
        [InlineData("v in ('1\\'',  '2' ,'3', '4')", " v in ('1\\'','2','3','4') ", typeof(InOperaterStatement))]
        [InlineData("v in (true,false)", " v in (true,false) ", typeof(InOperaterStatement))]
        [InlineData("xx = true", " xx = true ", typeof(OperaterStatement))]
        [InlineData("xx <= 3", " xx <= 3 ", typeof(OperaterStatement))]
        [InlineData("xx >= 3", " xx >= 3 ", typeof(OperaterStatement))]
        [InlineData("xx > 3", " xx > 3 ", typeof(OperaterStatement))]
        [InlineData("xx != 3", " xx != 3 ", typeof(OperaterStatement))]
        [InlineData("xx != 'sdsd != s'", " xx != 'sdsd != s' ", typeof(OperaterStatement))]
        [InlineData("yy like '%s%'", " yy like '%s%' ", typeof(OperaterStatement))]
        [InlineData("yy like 's%'", " yy like 's%' ", typeof(OperaterStatement))]
        [InlineData("yy like '%s'", " yy like '%s' ", typeof(OperaterStatement))]
        [InlineData("yy = NULL ", " yy = null ", typeof(OperaterStatement))]
        [InlineData("1 = 1", " 1 = 1 ", typeof(OperaterStatement))]
        [InlineData("1 = 1 and 2 != 3 or 11 >= 13.1 or 23 <= 31", " ( ( ( 1 = 1  and  2 != 3 )  or  11 >= 13.1 )  or  23 <= 31 ) ", typeof(ConditionsStatement))]
        [InlineData("11 >= 13.1 or 23 <= 31 ", " ( 11 >= 13.1  or  23 <= 31 ) ", typeof(ConditionsStatement))]
        [InlineData("((11 >= 13.1) or (23 <= 31 ))", " ( 11 >= 13.1  or  23 <= 31 ) ", typeof(ConditionsStatement))]
        [InlineData("((11 >= 13.1 and 1 != 2) or (23 <= 31  or x != y ))", " ( ( 11 >= 13.1  and  1 != 2 )  or  ( 23 <= 31  or  x != y ) ) ", typeof(ConditionsStatement))]
        [InlineData("(11 >= 13.1)", " 11 >= 13.1 ", typeof(OperaterStatement))]
        [InlineData("not(11 >= 13.1)", " not ( 11 >= 13.1 ) ", typeof(UnaryOperaterStatement))]
        [InlineData("(not(11 >= 13.1 and 1 != 2) or (23 <= 31  or x != y ))", " ( not ( ( 11 >= 13.1  and  1 != 2 ) )  or  ( 23 <= 31  or  x != y ) ) ", typeof(ConditionsStatement))]
        public void ShouldParse(string test, string expected, Type type)
        {
            TestStatement(test, statements =>
            {
                Assert.Single(statements);
                var t = statements[0];
                Assert.Equal(type, t.GetType());
                if (t is NumberValueStatement nv)
                {
                    Assert.Equal(expected, nv.Value.ToString());
                }
                else if (t is StringValueStatement s)
                {
                    Assert.Equal(expected, s.Value);
                }
                else if (t is BooleanValueStatement b)
                {
                    Assert.Equal(bool.Parse(expected), b.Value);
                }
                else if (t is FieldStatement f)
                {
                    Assert.Equal(expected, f.Field);
                }
                else if (t is OperaterStatement op)
                {
                    var sb = new StringBuilder();
                    From.ParseConditionStatementToQuery(sb, op);
                    Assert.Equal(expected, sb.ToString());
                }
                else if (t is InOperaterStatement na)
                {
                    var sb = new StringBuilder();
                    From.ParseConditionStatementToQuery(sb, na);
                    Assert.Equal(expected, sb.ToString());
                }
                else if (t is ConditionsStatement cs)
                {
                    var sb = new StringBuilder();
                    From.ParseConditionStatementToQuery(sb, cs);
                    Assert.Equal(expected, sb.ToString());
                }
                else if (t is UnaryOperaterStatement us)
                {
                    var sb = new StringBuilder();
                    From.ParseConditionStatementToQuery(sb, us);
                    Assert.Equal(expected, sb.ToString());
                }
            });
        }

        private void TestStatement(string v, Action<Statement[]> action)
        {
            var statements = SqlStatementParser.ParseStatements(v).ToArray();
            action(statements);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("*", "*")]
        [InlineData("a", "a")]
        [InlineData("a_d,b", "b,a_d")]
        [InlineData("a,b,json(a,'$.s',d)", "json(a,'$.s',d),b,a")]
        [InlineData("a,b,json(a,'$.s')", "json(a,'$.s'),b,a")]
        public void ShouldParseFields(string test, string expected)
        {
            var statements = SqlStatementParser.ParseStatements(test, ParseType.SelectField).Cast<FieldStatement>().ToArray();
            var sb = new StringBuilder();
            From.ParseFields(statements.ToList(), sb);
            Assert.Equal(expected, sb.ToString());
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "a Asc")]
        [InlineData("a asc,b", "b Asc,a Asc")]
        [InlineData("a,b,json(a,'$.s',d) desc", "json(a,'$.s',d) Desc,b Asc,a Asc")]
        [InlineData("a,b,json(a,'$.[s]') desc", "json(a,'$.[s]') Desc,b Asc,a Asc")]
        public void ShouldParseOrderByFields(string test, string expected)
        {
            var statements = SqlStatementParser.ParseStatements(test, ParseType.OrderByField).Cast<FieldStatement>().ToArray();
            var sb = new StringBuilder();
            From.ParseFields(statements.ToList(), sb);
            Assert.Equal(expected, sb.ToString());
        }
    }
}
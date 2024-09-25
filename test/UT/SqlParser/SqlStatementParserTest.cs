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
        [Fact]
        public void TestParse()
        {
            TestCondition("", condition =>
            {
                Assert.Null(condition);
            });
        }

        private void TestCondition(string v, Action<ConditionStatement> action)
        {
            action(SqlStatementParser.ParseWhereConditionStatement(v));
        }
    }
}
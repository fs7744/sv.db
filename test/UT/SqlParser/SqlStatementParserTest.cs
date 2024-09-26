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
        public void TestParseToken()
        {
            TestToken("", tokens =>
            {
                Assert.Empty(tokens);
            });
        }

        [Theory]
        [InlineData("1", "1")]
        [InlineData(" 2 ", "2")]
        [InlineData(" -3 ", "-3")]
        [InlineData("-4", "-4")]
        [InlineData("-5.6", "-5.6")]
        [InlineData("0.789", "0.789")]
        [InlineData("\r\t\n\r\t\n0.789   \r\t\n\r\t\n", "0.789")]
        public void ShouldParseNumber(string test, string expected)
        {
            TestToken(test, tokens =>
            {
                Assert.Single(tokens);
                var t = tokens[0];
                Assert.Equal(TokenType.Number, t.Type);
                Assert.Equal(expected, t.GetValue());
            });
        }

        [Theory]
        //[InlineData("-1-", "Can't parse near by --1 (Line:0,Col:0)")]
        [InlineData(" 2.3.4 ", "Can't parse near by 2.3.4  (Line:0,Col:1)")]
        public void ShouldNotParseNumber(string test, string expected)
        {
            var ex = Assert.Throws<ParserExecption>(() =>
            {
                TestToken(test, tokens =>
                {
                });
            });
            Assert.Equal(expected, ex.Message);
        }

        [Theory]
        [InlineData("k", "k")]
        [InlineData(" k1 ", "k1")]
        [InlineData(" k$1 ", "k$1")]
        public void ShouldParseWord(string test, string expected)
        {
            TestToken(test, tokens =>
            {
                Assert.Single(tokens);
                var t = tokens[0];
                Assert.Equal(TokenType.Word, t.Type);
                Assert.Equal(expected, t.GetValue());
            });
        }

        private void TestToken(string v, Action<List<Token>> action)
        {
            action(SqlStatementParser.ParseTokens(v).ToList());
        }
    }
}
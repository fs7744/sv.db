namespace SV.Script.Tests;

public class ParserTest
{
    private static string E(string src) => TestEngine.Eval(src);

    private static string W(string src) => TestEngine.EvalWith(src, TestEngine.SampleOrder());

    // ---------------------------------------------------------------- 后缀链组合

    [Fact]
    public void MemberThenIndex() => Assert.Equal("blue", W("return order.Tags[1];"));

    [Fact]
    public void MemberThenCall() => Assert.Equal("X-9:200", W("return order.Describe();"));

    [Fact]
    public void CallThenMember() => Assert.Equal("7", W("return order.Describe().Length;"));

    [Fact]
    public void CallThenIndex() => Assert.Equal("X", W("return order.Describe()[0];"));

    [Fact]
    public void CallThenCall() => Assert.Equal("X-9", W("return order.Describe().Substring(0, 3);"));

    [Fact]
    public void IndexThenMember() => Assert.Equal("4", W("return order.Tags[1].Length;"));

    [Fact]
    public void IndexThenCall() => Assert.Equal("BLUE", W("return order.Tags[1].ToUpperInvariant();"));

    [Fact]
    public void IndexThenIndex() => Assert.Equal("u", W("return order.Tags[1][2];"));

    [Fact]
    public void MemberChainThenCallThenMember()
        => Assert.Equal("3", W("return order.Customer.Name.ToUpperInvariant().Length;"));

    [Fact]
    public void LongMixedChain()
        => Assert.Equal("R", W("return order.Tags[0].ToUpperInvariant().Substring(0, 1);"));

    [Fact]
    public void ParenthesizedExpressionAcceptsPostfix()
        => Assert.Equal("2", E("return (\"ab\").Length;"));

    [Fact]
    public void ArrayLiteralAcceptsPostfix()
        => Assert.Equal("3", E("return [1, 2, 3].Count;"));

    [Fact]
    public void MapLiteralAcceptsPostfixInsideParens()
        => Assert.Equal("1", E("let m = { a: 1 }; return m.a;"));

    [Fact]
    public void ChainInsideArgument()
        => Assert.Equal("3", W("return order.Tags[0].Length + 0;"));

    [Fact]
    public void NestedCallArguments()
        => Assert.Equal("2.35", E("return Math.Round(Math.Round(2.3456, 3), 2);"));

    [Fact]
    public void CallArgumentsAreExpressions()
        => Assert.Equal("40.0", W("return order.Discount(0.1 + 0.1);"));

    [Fact]
    public void IndexArgumentIsAnExpression()
        => Assert.Equal("blue", W("let i = 0; return order.Tags[i + 1];"));

    // ---------------------------------------------------------------- 括号与嵌套

    [Fact]
    public void DeeplyNestedParens()
        => Assert.Equal("6", E("return ((((1)) + ((2)) + (((3)))));"));

    [Fact]
    public void ParensOverridePrecedence()
    {
        Assert.Equal("9", E("return (1 + 2) * 3;"));
        Assert.Equal("7", E("return 1 + (2 * 3);"));
        Assert.Equal("1", E("return (1 + 2) / 3;"));
    }

    [Fact]
    public void RedundantParensAreHarmless()
        => Assert.Equal("true", E("return ((((true))));"));

    [Fact]
    public void ParensAroundConditional()
        => Assert.Equal("2", E("return (true ? 1 : 0) + 1;"));

    [Fact]
    public void NestedTernaries()
        => Assert.Equal("mid", E("let n = 5; return n < 3 ? \"low\" : (n < 8 ? \"mid\" : \"high\");"));

    // ---------------------------------------------------------------- 排版无关

    [Fact]
    public void EverythingOnOneLine()
        => Assert.Equal("3", E("let a=1;let b=2;return a+b;"));

    [Fact]
    public void ExtraWhitespaceAndNewlines()
        => Assert.Equal("3", E("""

            let   a   =   1  ;

                let b
                    =
                        2 ;

            return
                a
                +
                b ;

            """));

    [Fact]
    public void NoSpacesAroundOperators()
        => Assert.Equal("7", E("return 1+2*3;"));

    [Fact]
    public void TabsAsIndentation()
        => Assert.Equal("1", E("if (true) {\n\t\treturn 1;\n}\nreturn 0;"));

    [Fact]
    public void CrlfLineEndings()
        => Assert.Equal("3", E("let a = 1;\r\nlet b = 2;\r\nreturn a + b;\r\n"));

    [Fact]
    public void SemicolonImmediatelyAfterBlock()
        => Assert.Equal("1", E("if (true) { return 1; };"));

    // ---------------------------------------------------------------- 注释穿插

    [Fact]
    public void CommentsBetweenTokens()
        => Assert.Equal("3", E("let /*x*/ a /*y*/ = /*z*/ 1 + 2; return a;"));

    [Fact]
    public void CommentInsideCallArguments()
        => Assert.Equal("2.34", E("return Math.Round(2.345 /* 值 */, 2 /* 精度 */);"));

    [Fact]
    public void CommentInsideBlock()
        => Assert.Equal("1", E("""
            if (true) {
                // 先注释一行
                return 1;   // 尾注释
                /* 再来个块注释 */
            }
            return 0;
            """));

    [Fact]
    public void CommentBetweenIfAndElse()
        => Assert.Equal("2", E("if (false) { return 1; } /* 中间 */ else { return 2; }"));

    [Fact]
    public void DivisionIsNotConfusedWithComment()
        => Assert.Equal("2", E("return 4 / 2;"));

    [Fact]
    public void CommentLikeContentInsideString()
        => Assert.Equal("a//b", E("return \"a//b\";"));

    [Fact]
    public void BlockCommentMarkersInsideString()
        => Assert.Equal("a/*b*/c", E("return \"a/*b*/c\";"));

    // ---------------------------------------------------------------- 语句边界

    [Fact]
    public void BlockAsStatementIsNotAMapLiteral()
        => Assert.Equal("1", E("{ let a = 1; return a; }"));

    [Fact]
    public void ConsecutiveBlocks()
        => Assert.Equal("3", E("let s = 0; { s += 1; } { s += 2; } return s;"));

    [Fact]
    public void NestedBlocks()
        => Assert.Equal("1", E("{ { { return 1; } } }"));

    [Fact]
    public void CallStatementNeedsSemicolon()
        => Assert.NotEmpty(TestEngine.FailsToCompile("order.Bump()"));

    [Fact]
    public void MultipleStatementsOnOneLine()
        => Assert.Equal("6", E("let a = 1; let b = 2; let c = 3; return a + b + c;"));

    // ---------------------------------------------------------------- 综合读一段真实脚本

    [Fact]
    public void MultiLineRealisticScriptParses()
    {
        const string src = """
            // 运费规则
            let base = 5;
            let weight = order.Weight;

            if (weight <= 1) {
                base = 5;
            } else if (weight <= 5) {
                base = 5 + (weight - 1) * 2;      // 超出部分每公斤 2 元
            } else {
                base = 13 + (weight - 5) * 1.5;
            }

            /* VIP 免运费 */
            if (order.Customer?.IsVip == true) {
                return 0;
            }

            foreach (t in order.Tags) {
                if (t == "fragile") {
                    base += 3;
                    break;
                }
            }

            return Math.Round(base, 2);
            """;

        var order = TestEngine.SampleOrder();
        order.Customer!.IsVip = false;
        order.Tags.Add("fragile");
        // 1.5kg -> 5 + (1.5-1)*2 = 6.0，带 fragile 再 +3 = 9.0
        Assert.Equal("9.0", TestEngine.EvalWith(src, order));

        order.Customer.IsVip = true;
        Assert.Equal("0", TestEngine.EvalWith(src, order));
    }
}

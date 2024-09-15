using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class TupleTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.CreateCommand().SetParams((3, 56));
            var d = (F: 3, u: 3);
            connection.CreateCommand().SetParams(d);
            connection.CreateCommand().SetParams(d);
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class ValueTuple_", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("p.ParameterName = \"Item1\";", generatedCode);
            Assert.Contains("p.ParameterName = \"Item2\";", generatedCode);
            Assert.Contains("p.ParameterName = \"F\";", generatedCode);
            Assert.Contains("p.ParameterName = \"u\";", generatedCode);
            Assert.Contains("public static readonly RecordFactory<(int, int)> Instance = new ValueTuple_", generatedCode);
            Assert.Contains("public static readonly RecordFactory<(int F, int u)> Instance = new ValueTuple_", generatedCode);
        }
    }
}
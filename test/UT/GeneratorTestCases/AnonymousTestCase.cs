using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class AnonymousTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.CreateCommand().SetParams(new { });
            connection.CreateCommand().SetParams(new { F = 3, u = 3 });
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class Anonymous_", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("p.ParameterName = \"F\";", generatedCode);
            Assert.Contains("p.ParameterName = \"u\";", generatedCode);
        }
    }
}
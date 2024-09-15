using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class ExecuteReaderTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.ExecuteReader("", new { a = 3 });
            connection.ExecuteReaderAsync("", (3, 4));
            var cmd = connection.CreateCommand();
            cmd.ExecuteReader(new { a = 3 });
            cmd.ExecuteReaderAsync((3, 4));
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            Assert.Contains("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
        }
    }
}
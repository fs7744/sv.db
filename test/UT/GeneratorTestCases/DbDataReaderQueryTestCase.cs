using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class DbDataReaderQueryTestCase
    {
        public void TestCase()
        {
            DbDataReader? reader = default;
            reader.Query<(int, int)>();
            reader.QueryAsync<(int, int)>();
            reader.QueryFirstOrDefault<(int, int)>();
            reader.QueryFirstOrDefaultAsync<(int, int)>();
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.DoesNotContain("var cmd = connection.CreateCommand();", generatedCode);
            Assert.DoesNotContain("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
        }
    }
}
using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class EmptyClassSetTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams<EmptyClassTestData>(new EmptyClassTestData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class EmptyClassTestData_", generatedCode);
            Assert.DoesNotContain("p.ParameterName =", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.EmptyClassTestData>(new EmptyClassTestData_", generatedCode);
        }
    }

    public class EmptyClassTestData
    { }
}
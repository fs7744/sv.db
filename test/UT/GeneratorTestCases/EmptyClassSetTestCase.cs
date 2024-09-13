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
        }
    }

    public class EmptyClassTestData
    { }
}
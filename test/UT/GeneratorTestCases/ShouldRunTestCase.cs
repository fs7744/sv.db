using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class ShouldRunTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.ExecuteNonQueryAsync("", "").GetAwaiter().GetResult();
        }

        public void Check(string generatedCode)
        {
            Assert.Contains("internal static void InitFunc()", generatedCode);
        }
    }
}
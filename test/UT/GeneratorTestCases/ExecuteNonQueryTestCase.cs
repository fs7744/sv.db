using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class ExecuteNonQueryTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.ExecuteNonQuery("");
            connection.ExecuteNonQuery("", new { a = 3 });
            connection.ExecuteNonQueryAsync("");
            connection.ExecuteNonQueryAsync("", (3, 4));
            var cmd = connection.CreateCommand();
            cmd.ExecuteNonQuery(new { a = 3 });
            cmd.ExecuteNonQueryAsync((3, 4));
            cmd.ExecuteNonQuery();
            cmd.ExecuteNonQueryAsync();
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            Assert.Contains("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteNonQuery", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteNonQueryAsync", generatedCode);
        }
    }
}
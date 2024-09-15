using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class ExecuteScalarTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.ExecuteScalar("");
            connection.ExecuteScalar("", new { a = 3 });
            connection.ExecuteScalarAsync("");
            connection.ExecuteScalarAsync("", (3, 4));
            connection.ExecuteScalar<int>("");
            connection.ExecuteScalar<int>("", new { a = 3 });
            connection.ExecuteScalarAsync<DateTime>("");
            connection.ExecuteScalarAsync<DateTime>("", (3, 4));
            var cmd = connection.CreateCommand();
            cmd.ExecuteScalar(new { a = 3 });
            cmd.ExecuteScalarAsync((3, 4));
            cmd.ExecuteScalar<int>( new { a = 3 });
            cmd.ExecuteScalarAsync<DateTime>( (3, 4));
            cmd.ExecuteScalar();
            cmd.ExecuteScalarAsync();
            cmd.ExecuteScalar<int>();
            cmd.ExecuteScalarAsync<DateTime>();
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.DoesNotContain("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            Assert.Contains("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteScalarAsync", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteScalarObjectAsync", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteScalar", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteScalarObject", generatedCode);
        }
    }
}
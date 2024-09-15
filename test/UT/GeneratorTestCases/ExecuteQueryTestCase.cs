using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class ExecuteQueryTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.ExecuteQuery<ClassTestData>("");
            connection.ExecuteQueryAsync<ClassTestData>("");
            connection.ExecuteQuery<ClassTestData>("", new { a = 3 });
            connection.ExecuteQueryAsync<ClassTestData>("", (3, 4));
            connection.ExecuteQuery<int>("");
            connection.ExecuteQueryAsync<int>("");
            connection.ExecuteQuery<int>("", new { a = 3 });
            connection.ExecuteQueryAsync<int>("", (3, 4));
            connection.ExecuteQuery<StructTestCaseData>("");
            connection.ExecuteQueryAsync<StructTestCaseData>("");
            connection.ExecuteQuery<StructTestCaseData>("", new { a = 3 });
            connection.ExecuteQueryAsync<StructTestCaseData>("", (3, 4));
            var cmd = connection.CreateCommand();
            cmd.ExecuteQuery<int>(new { a = 3 });
            cmd.ExecuteQueryAsync<int>((3, 4));
            cmd.ExecuteQuery<int>();
            cmd.ExecuteQueryAsync<int>();
            cmd.ExecuteQuery<ClassTestData>(new { a = 3 });
            cmd.ExecuteQueryAsync<ClassTestData>((3, 4));
            cmd.ExecuteQuery<ClassTestData>();
            cmd.ExecuteQueryAsync<ClassTestData>();
            cmd.ExecuteQuery<StructTestCaseData>(new { a = 3 });
            cmd.ExecuteQueryAsync<StructTestCaseData>((3, 4));
            cmd.ExecuteQuery<StructTestCaseData>();
            cmd.ExecuteQueryAsync<StructTestCaseData>();
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            Assert.Contains("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteQuery", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteQueryAsync", generatedCode);
        }
    }
}
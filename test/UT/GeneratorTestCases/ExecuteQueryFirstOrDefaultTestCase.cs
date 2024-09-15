using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class ExecuteQueryFirstOrDefaultTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.ExecuteQueryFirstOrDefault<ClassTestData>("");
            connection.ExecuteQueryFirstOrDefaultAsync<ClassTestData>("");
            connection.ExecuteQueryFirstOrDefault<ClassTestData>("", new { a = 3 });
            connection.ExecuteQueryFirstOrDefaultAsync<ClassTestData>("", (3, 4));
            connection.ExecuteQueryFirstOrDefault<int>("");
            connection.ExecuteQueryFirstOrDefaultAsync<int>("");
            connection.ExecuteQueryFirstOrDefault<int>("", new { a = 3 });
            connection.ExecuteQueryFirstOrDefaultAsync<int>("", (3, 4));
            connection.ExecuteQueryFirstOrDefault<StructTestCaseData>("");
            connection.ExecuteQueryFirstOrDefaultAsync<StructTestCaseData>("");
            connection.ExecuteQueryFirstOrDefault<StructTestCaseData>("", new { a = 3 });
            connection.ExecuteQueryFirstOrDefaultAsync<StructTestCaseData>("", (3, 4));
            var cmd = connection.CreateCommand();
            cmd.ExecuteQueryFirstOrDefault<int>(new { a = 3 });
            cmd.ExecuteQueryFirstOrDefaultAsync<int>((3, 4));
            cmd.ExecuteQueryFirstOrDefault<int>();
            cmd.ExecuteQueryFirstOrDefaultAsync<int>();
            cmd.ExecuteQueryFirstOrDefault<ClassTestData>(new { a = 3 });
            cmd.ExecuteQueryFirstOrDefaultAsync<ClassTestData>((3, 4));
            cmd.ExecuteQueryFirstOrDefault<ClassTestData>();
            cmd.ExecuteQueryFirstOrDefaultAsync<ClassTestData>();
            cmd.ExecuteQueryFirstOrDefault<StructTestCaseData>(new { a = 3 });
            cmd.ExecuteQueryFirstOrDefaultAsync<StructTestCaseData>((3, 4));
            cmd.ExecuteQueryFirstOrDefault<StructTestCaseData>();
            cmd.ExecuteQueryFirstOrDefaultAsync<StructTestCaseData>();
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory", generatedCode);
            Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            Assert.Contains("Anonymous_", generatedCode);
            Assert.Contains("ValueTuple_", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteQueryFirstOrDefault", generatedCode);
            Assert.Contains("return CommandExtensions.DbCommandExecuteQueryFirstOrDefaultAsync", generatedCode);
        }
    }
}
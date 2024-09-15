using SV.Db;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;

namespace UT.GeneratorTestCases
{
    internal class ExecuteNonQuerysTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            List<ClassTestData> classTestDatas = null;
            connection.ExecuteNonQuerys("", classTestDatas);
            connection.ExecuteNonQuerysAsync("", classTestDatas);
            classTestDatas = new List<ClassTestData>();
            connection.ExecuteNonQuerys("", classTestDatas);
            connection.ExecuteNonQuerysAsync("", classTestDatas);
            classTestDatas = new List<ClassTestData>() { new ClassTestData()};
            connection.ExecuteNonQuerys("", classTestDatas);
            connection.ExecuteNonQuerysAsync("", classTestDatas);
            classTestDatas = new List<ClassTestData>() { new ClassTestData(), new ClassTestData() };
            connection.ExecuteNonQuerys("", classTestDatas);
            connection.ExecuteNonQuerysAsync("", classTestDatas);

            ClassTestData[] classTestDatas2 = null;
            connection.ExecuteNonQuerys("", classTestDatas2);
            connection.ExecuteNonQuerysAsync("", classTestDatas2);
            classTestDatas2 = new ClassTestData[0];
            connection.ExecuteNonQuerys("", classTestDatas2);
            connection.ExecuteNonQuerysAsync("", classTestDatas2);
            classTestDatas2 = new ClassTestData[] { new ClassTestData() };
            connection.ExecuteNonQuerys("", classTestDatas2);
            connection.ExecuteNonQuerysAsync("", classTestDatas2);
            classTestDatas2 = new ClassTestData[] { new ClassTestData(), new ClassTestData() };
            connection.ExecuteNonQuerys("", classTestDatas2);
            connection.ExecuteNonQuerysAsync("", classTestDatas2);

           IEnumerable<ClassTestData> classTestDatas22 = null;
            connection.ExecuteNonQuerys("", classTestDatas22);
            connection.ExecuteNonQuerysAsync("", classTestDatas22);
            classTestDatas22 = new ClassTestData[0];
            connection.ExecuteNonQuerys("", classTestDatas22);
            connection.ExecuteNonQuerysAsync("", classTestDatas22);
            classTestDatas22 = new ClassTestData[] { new ClassTestData() };
            connection.ExecuteNonQuerys("", classTestDatas22);
            connection.ExecuteNonQuerysAsync("", classTestDatas22);
            classTestDatas22 = new ClassTestData[] { new ClassTestData(), new ClassTestData() };
            connection.ExecuteNonQuerys("", classTestDatas22);
            connection.ExecuteNonQuerysAsync("", classTestDatas22);

            connection.ExecuteNonQuerys("", new (int, int)[] { (2,3) });
            connection.ExecuteNonQuerysAsync("", new (int, int)[] { (2, 3) }.Select(i => new { a = i.Item1}));
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.ClassTestData>(new ClassTestData_", generatedCode);
            //Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", generatedCode);
            //Assert.Contains("var cmd = connection.CreateCommand();", generatedCode);
            //Assert.Contains("Anonymous_", generatedCode);
            //Assert.Contains("ValueTuple_", generatedCode);
            //Assert.Contains("return CommandExtensions.DbCommandExecuteNonQuery", generatedCode);
            //Assert.Contains("return CommandExtensions.DbCommandExecuteNonQueryAsync", generatedCode);
        }
    }
}
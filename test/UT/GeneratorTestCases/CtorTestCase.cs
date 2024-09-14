using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class CtorTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new CtorTestCaseData(default, default));
            connection.CreateCommand().SetParams(new CtorTestCaseData2(default, default));
            connection.CreateCommand().SetParams(new CtorTestCaseData3(default, default, default));
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class CtorTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.CtorTestCaseData>(new CtorTestCaseData_", generatedCode);
            Assert.Contains("public class CtorTestCaseData2_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.CtorTestCaseData2>(new CtorTestCaseData2_", generatedCode);
            Assert.Contains("public class CtorTestCaseData3_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.CtorTestCaseData3>(new CtorTestCaseData3_", generatedCode);
            Assert.Contains("new global::UT.GeneratorTestCases.CtorTestCaseData3(default, default, default);", generatedCode);
        }
    }

    public class CtorTestCaseData
    {
        public CtorTestCaseData(CtorTestCaseData a, int d)
        {
        }

        public CtorTestCaseData(CtorTestCaseData a, int? d)
        {
        }
    }

    public class CtorTestCaseData3
    {
        public CtorTestCaseData3(CtorTestCaseData a, int d)
        {
        }

        public CtorTestCaseData3(CtorTestCaseData a, int? d)
        {
        }

        [Ctor]
        public CtorTestCaseData3(CtorTestCaseData a, int? d, int a3)
        {
        }
    }

    public struct CtorTestCaseData2
    {
        public CtorTestCaseData2(CtorTestCaseData a, int d)
        {
        }

        public CtorTestCaseData2(CtorTestCaseData a, int? d)
        {
        }
    }
}
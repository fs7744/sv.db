using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class GuidTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new GuidTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class GuidTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.GuidTestCaseData>(new GuidTestCaseData", generatedCode);
            Assert.Contains("p.Value = args.Int.HasValue ? args.Int.Value : DBNull.Value;", generatedCode);
        }
    }

    public class GuidTestCaseData
    {
        public Guid Int1 { get; set; }

        public Guid Int21;
        public Guid? Int { get; set; }

        public Guid? Int2;
    }
}
using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class DateTimeFieldTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new DateTimeFieldTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class DateTimeFieldTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.DateTimeFieldTestCaseData>(new DateTimeFieldTestCaseData_", generatedCode);
            Assert.Contains("p.Value = args.Int.HasValue ? args.Int.Value : DBNull.Value;", generatedCode);
        }
    }

    public class DateTimeFieldTestCaseData
    {
        public DateTime Int1 { get; set; }

        public DateTime Int21;
        public DateTime? Int { get; set; }

        public DateTime? Int2;
    }
}
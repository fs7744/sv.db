using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class EnumTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new EnumTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class EnumTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.EnumTestCaseData>(new EnumTestCaseData", generatedCode);
            Assert.Contains("p.Value = args.Int;", generatedCode);
            Assert.Contains("d.Int = reader.IsDBNull(j) ? default : DBUtils.ToEnum<global::UT.GeneratorTestCases.EnumTestCaseData2>(reader.GetValue(j));", generatedCode);
            Assert.Contains("d.Int2 = reader.IsDBNull(j) ? default : DBUtils.ToEnum<global::UT.GeneratorTestCases.EnumTestCaseData2>(reader.GetValue(j));", generatedCode);
            Assert.Contains("p.Value = args.Int2.HasValue ? args.Int2.Value : DBNull.Value;", generatedCode);
        }
    }

    public enum EnumTestCaseData2
    {
        None = 0,
        d = 1,
        bb = 2
    }

    public class EnumTestCaseData
    {
        public EnumTestCaseData2 Int { get; set; }

        public EnumTestCaseData2? Int2;
    }
}
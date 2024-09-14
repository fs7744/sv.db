using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class StructTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams<StructTestCaseData>(new StructTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class StructTestCaseData_", generatedCode);
            Assert.Contains("p.ParameterName = \"Id\";", generatedCode);
            Assert.DoesNotContain("p.ParameterName = \"Id2\";", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.StructTestCaseData>(new StructTestCaseData_", generatedCode);
        }
    }

    public struct StructTestCaseData
    {
        private int Id2 { get; set; }
        public int Id { get; set; }
        public Int16 Idd { get; set; }
        public Int64 Idd2 { get; set; }
        public UInt16 Idd3 { get; set; }
        public UInt64 Idd21 { get; set; }
        public UInt32 Idd1 { get; set; }
        public Boolean Boolean { get; set; }
        public String String { get; set; }
        public SByte SByte { get; set; }
        public Byte Byte { get; set; }
        public Char Char { get; set; }
        public Decimal Decimal { get; set; }
        public Single Single { get; set; }
        public Double Double { get; set; }
        public DateTime DateTime { get; set; }

        private DateTime Idff22;
        public int Idff;
        public Int16 Iddff;
        public Int64 Idd2ff;
        public UInt16 Idd3ff;
        public UInt64 Idd21ff;
        public UInt32 Idd1ff;
        public Boolean Booleanff;
        public String Stringff;
        public SByte SByteff;
        public Byte Byteff;
        public Char Charff;
        public Decimal Decimalff;
        public Single Singleff;
        public Double Doubleff;
        public DateTime DateTimeff;
    }
}
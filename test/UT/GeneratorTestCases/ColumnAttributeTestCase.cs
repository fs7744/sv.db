using SV;
using SV.Db;
using System.Data;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class ColumnAttributeTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new ColumnAttributeTestData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class ColumnAttributeTestData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.ColumnAttributeTestData>(new ColumnAttributeTestData_", generatedCode);
            Assert.DoesNotContain("p.ParameterName = \"A2\";", generatedCode);
            Assert.DoesNotContain("p.ParameterName = \"A32\";", generatedCode);
            Assert.Contains("p.ParameterName = \"AAA\";", generatedCode);
            Assert.Contains("p.DbType = System.Data.DbType.AnsiString;", generatedCode);
            Assert.Contains("p.Direction = System.Data.ParameterDirection.InputOutput;", generatedCode);
            Assert.Contains("p.Value = UT.GeneratorTestCases.ColumnAttributeTestData.C(args.Col);", generatedCode);
            Assert.Contains("p.Precision = 3;", generatedCode);
            Assert.Contains("p.Scale = 5;", generatedCode);
            Assert.Contains("p.Size = 6;", generatedCode);
            Assert.Contains("p.Precision = 3;", generatedCode);
            Assert.Contains("p.ParameterName = \"AAAd\";", generatedCode);
            Assert.Contains("p.Value = args.A;", generatedCode);
            Assert.Contains("p.DbType = DbType.String;", generatedCode);
            Assert.Contains($"case {StringHashing.HashOrdinalIgnoreCase("AAA")}:", generatedCode);
            Assert.Contains($"case {StringHashing.HashOrdinalIgnoreCase("AAAd")}:", generatedCode);
            Assert.Contains("d.Col = reader.IsDBNull(j) ? default : UT.GeneratorTestCases.ColumnAttributeTestData.C2(reader.GetValue(j));", generatedCode);
        }
    }

    public class ColumnAttributeTestData
    {
        [Column(Name = "AAA", Type = DbType.AnsiString, Direction = ParameterDirection.InputOutput, Precision = 3, Scale = 5, Size = 6, CustomConvertToDbMethod = $"UT.GeneratorTestCases.ColumnAttributeTestData.C", CustomConvertFromDbMethod = $"UT.GeneratorTestCases.ColumnAttributeTestData.C2")]
        public List<int>? Col { get; set; }

        [Column(Name = "AAAd")]
        public string? A;

        [NotColumn]
        public string? A2;

        [NotColumn]
        public string? A32 { get; set; }

        public static object C(object c)
        {
            return c;
        }

        public static List<int> C2(object c)
        {
            return null;
        }
    }
}
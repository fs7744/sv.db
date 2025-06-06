﻿using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class NullableFieldTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams(new NullableFieldTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class NullableFieldTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.NullableFieldTestCaseData>(new NullableFieldTestCaseData_", generatedCode);
            Assert.Contains("p.Value = args.Int.HasValue ? args.Int.Value : DBNull.Value;", generatedCode);
            Assert.Contains("d.Int = reader.IsDBNull(j) ? default : reader.GetInt32(j);", generatedCode);
            Assert.Contains("d.Int = reader.IsDBNull(j) ? default : DBUtils.As<int>(reader.GetValue(j));", generatedCode);
            Assert.Contains("p.Value = args.Int2.HasValue ? args.Int2.Value : DBNull.Value;", generatedCode);
        }
    }

    public class NullableFieldTestCaseData
    {
        public int? Int { get; set; }

        public decimal? Int2;
    }
}
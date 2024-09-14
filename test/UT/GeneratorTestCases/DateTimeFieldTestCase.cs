using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using SV.Db;

namespace UT.GeneratorTestCases
{
    internal class DateTimeFieldTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.CreateCommand().SetParams<DateTimeFieldTestCaseData>(new DateTimeFieldTestCaseData());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class DateTimeFieldTestCaseData_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.DateTimeFieldTestCaseData>(new DateTimeFieldTestCaseData_", generatedCode);
            Assert.Contains("p.Value = args.Int.GetValueOrDefault();", generatedCode);
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
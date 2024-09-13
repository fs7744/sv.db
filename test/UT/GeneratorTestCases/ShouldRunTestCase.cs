﻿using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class ShouldRunTestCase
    {
        public void TestCase()
        {
            DbConnection connection = null;
            connection.ExecuteNonQueryAsync("", "");
        }

        public void Check(string generatedCode)
        {
            Assert.Contains("// total: 0", generatedCode);
        }
    }
}
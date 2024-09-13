using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SV.Db;
using System.Data.Common;
using Xunit;

namespace UT.GeneratorTestCases
{
    public class ShouldRunTestCase
    {
        public void Main(string[] args)
        {
            DbConnection connection = null;
            connection.ExecuteQueryFirstOrDefault<string>("select * from dog", new TestDd());
            connection.CreateCommand().SetParams<string>(string.Empty);
            connection.ExecuteNonQueryAsync("", (2, Name: 3));
            var b = (4, Name: 3);
            connection.ExecuteNonQueryAsync("", b);
            object c = (4, Name: 3);
            connection.ExecuteNonQueryAsync("", args: c);

            connection.ExecuteNonQueryAsync("", "");
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
        }
    }
}
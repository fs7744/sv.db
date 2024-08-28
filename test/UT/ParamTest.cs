using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SV.Db;

namespace UT
{
    public class ParamTest
    {
        [Fact]
        public void GenerateTest()
        {
            var connection = new TestDbConnection() { RowCount = 1, Data = new TestData(("a", "d")) };
            connection.ExecuteQueryFirstOrDefault<string>("select * from dog", new TestData(("a", "d")));
            connection.CreateCommand().SetParams<string>(string.Empty);
        }
    }
}
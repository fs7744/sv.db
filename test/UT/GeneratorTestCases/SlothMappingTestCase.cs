using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT.GeneratorTestCases
{
    public class SlothMappingTestCase
    {
        public void TestCase()
        {
            IConnectionFactory factory = null;
            factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ExecuteQueryAsync<Weather>();
        }

        public void Check(string generatedCode)
        {
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Weather>(new Weather_", generatedCode);
        }
    }

    [Db("Demo")]
    [Table(nameof(Weather))]
    public class Weather
    {
        [Select, Where, OrderBy]
        public string Name { get; set; }

        [Select(Field = "Value as v"), Where, OrderBy]
        public string V { get; set; }

        [Select(NotAllow = true)]
        public string Test { get; set; }
    }
}
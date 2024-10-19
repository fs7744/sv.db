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
            factory.ExecuteInsertAsync(new Weather2());
            factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ExecuteQueryAsync<Weather>();
        }

        public void Check(string generatedCode)
        {
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Weather>(new Weather_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Weather2>(new Weather2_", generatedCode);
        }
    }

    [Db("Demo")]
    [Table(nameof(Weather))]
    public class Weather
    {
        [Select("Name"), Where, OrderBy]
        public string Name { get; set; }

        [Select("Value as v"), Where, OrderBy]
        public string V { get; set; }

        [Select("Test", NotAllow = true)]
        public string Test { get; set; }
    }

    [Db("Demo")]
    [Table(nameof(Weather))]
    public class Weather2
    {
        [Select("Name"), Where, OrderBy]
        public string Name { get; set; }

        [Select("Value as v"), Where, OrderBy]
        public string V { get; set; }

        [Select("Test", NotAllow = true)]
        public string Test { get; set; }
    }
}
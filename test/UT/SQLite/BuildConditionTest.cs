using Microsoft.Extensions.Primitives;
using SV.Db;
using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using SV.Db.Sloth.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT.SQLite
{
    public class BuildConditionTest
    {
        [Fact]
        public void TestCases()
        {
            Assert.Equal("where Name = 33 ",
                Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NAME", "33" }
                }));

            Assert.Equal("where Name = false ",
                Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NAME", "false" }
                }));

            Assert.Equal("where Name = @P_0 ",
                Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NAME", "fsse" }
                }));
        }

        public string Build<T>(Dictionary<string, StringValues> ps)
        {
            var factory = new ConnectionStringProviders(new IConnectionStringProvider[] { DictionaryConnectionStringProvider.Instance }, null);
            var statement = factory.ParseByParams<T>(ps, out var info);
            var cmd = new TestDbCommand();
            return SQLiteConnectionProvider.BuildCondition(cmd, info, statement.Where.Condition);
        }
    }

    [Db("TestDemo")]
    [Table(nameof(BuildConditionTestData))]
    public class BuildConditionTestData
    {
        [Select]
        public string Name { get; set; }

        [Select(Field = "Value as v")]
        public string V { get; set; }
    }
}
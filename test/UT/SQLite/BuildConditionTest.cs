﻿using Microsoft.Extensions.Primitives;
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
                }, out var cmd));

            Assert.Equal("where Name = false ",
                Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NAME", "false" }
                }, out cmd));

            Assert.Equal("where Name = @P_0 ",
                Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NAME", "fsse" }
                }, out cmd));

            Assert.Equal("where Name = @P_0 ",
            Build<BuildConditionTestData>(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            {
                    { "NAME", "Pending" }
            }, out cmd));
            Assert.Equal("Pending", cmd.Parameters[0].Value);
        }

        public string Build<T>(Dictionary<string, StringValues> ps, out TestDbCommand cmd)
        {
            var factory = new ConnectionStringProviders(new IConnectionStringProvider[] { DictionaryConnectionStringProvider.Instance }, null, null);
            var statement = factory.ParseByParams<T>(ps, out var info);
            cmd = new TestDbCommand();
            return SQLiteConnectionProvider.BuildCondition(cmd, info, statement.Where.Condition);
        }
    }

    [Db("TestDemo")]
    [Table(nameof(BuildConditionTestData))]
    public class BuildConditionTestData
    {
        [Select("Name"), Where]
        public string Name { get; set; }

        [Select("Value")]
        public string V { get; set; }
    }
}
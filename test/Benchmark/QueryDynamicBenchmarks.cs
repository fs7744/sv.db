using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using SV.Db;

namespace Benchmark
{
    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class QueryDynamicBenchmarks
    {
        [Params(1, 1000, 10000, 100000, 1000000)]
        public int RowCount { get; set; }

        private TestData data = new TestData(("a", "oo"), ("b", 333), ("c", 333), ("a1", "oo"), ("b1", 333), ("c1", 333), ("a12", "oo"), ("b12", 333), ("c12", 333));

        [Benchmark(Baseline = true)]
        public List<dynamic?> DynamicExpandoObject()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            List<dynamic> dogs = new List<dynamic>();
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                {
                    var arr = new string[reader.FieldCount];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        arr[i] = reader.GetName(i);
                    }

                    while (reader.Read())
                    {
                        IDictionary<string, object> dog = new ExpandoObject();
                        dogs.Add(dog);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            dog[arr[i]] = reader.GetValue(i);
                        }
                    }
                }
            }
            finally
            {
                connection.Close();
            }
            return dogs;
        }

        [Benchmark]
        public List<dynamic?> ExecuteQueryRowCount()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<dynamic>("select ", estimateRow: RowCount));
        }

        [Benchmark]
        public List<dynamic?> ExecuteQuery()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<dynamic>("select "));
        }

        [Benchmark]
        public List<dynamic> DapperDynamic()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.Query("select * from dog"));
        }

        [Benchmark, DapperAot]
        public List<dynamic> DapperAotDynamic()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.Query("select * from dog"));
        }
    }
}
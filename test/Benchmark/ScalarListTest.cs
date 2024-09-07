using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Dapper;
using SV.Db;
using System.Data;

namespace Benchmark
{
    [MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class ScalarListTest
    {
        [Params(1, 10, 100, 1000, 10000, 100000, 1000000)]
        public int RowCount { get; set; }

        private TestData data = new TestData(("a", "oo"));

        [Benchmark(Baseline = true)]
        public List<string> GetStringList()
        {
            var dogs = new List<string>();
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                {
                    while (reader.Read())
                    {
                        dogs.Add(reader.GetString(0));
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
        public List<string?> ExecuteQueryRowCount()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<string>("select ", estimateRow: RowCount));
        }

        [Benchmark]
        public List<string?> ExecuteQuery()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<string>("select "));
        }

        [Benchmark]
        public List<string> Dapper()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return System.Linq.EnumerableExtensions.AsList(connection.Query<string>("select * from dog"));
        }

        [Benchmark, DapperAot]
        public List<string> DapperAot()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return System.Linq.EnumerableExtensions.AsList(connection.Query<string>("select * from dog"));
        }
    }

    [MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class ScalarOneTest
    {
        [Params(0, 1)]
        public int RowCount { get; set; }

        private TestData data = new TestData(("a", "oo"));

        [Benchmark(Baseline = true)]
        public string? GetString()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                    return null;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        [Benchmark]
        public string ExecuteScalar()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return connection.ExecuteScalar<string>("select ");
        }

        [Benchmark]
        public string? ExecuteQueryFirstOrDefault()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return connection.ExecuteQueryFirstOrDefault<string>("select ");
        }

        [Benchmark]
        public string? Dapper()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return SqlMapper.ExecuteScalar<string>(connection, "select * from dog");
        }

        [Benchmark, DapperAot]
        public string? DapperAot()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return SqlMapper.ExecuteScalar<string>(connection, "select * from dog");
        }

        [Benchmark]
        public string? DapperQueryFirstOrDefault()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return connection.QueryFirstOrDefault<string>("select * from dog");
        }

        [Benchmark, DapperAot]
        public string? DapperAotQueryFirstOrDefault()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return connection.QueryFirstOrDefault<string>("select * from dog");
        }
    }

    public enum TestEnum
    {
        oo = 3,
        ooo = 4,
    }

    [MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class ScalarEnumStringTest
    {
        [Params(1, 10, 100, 1000, 10000, 100000, 1000000)]
        public int RowCount { get; set; }

        private TestData data = new TestData(("a", "oo"));

        [Benchmark(Baseline = true)]
        public List<TestEnum> GetList()
        {
            var dogs = new List<TestEnum>();
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                {
                    while (reader.Read())
                    {
                        dogs.Add(Enum.Parse<TestEnum>(reader.GetString(0)));
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
        public List<TestEnum> ExecuteQueryRowCount()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<TestEnum>("select ", estimateRow: RowCount));
        }

        [Benchmark]
        public List<TestEnum> ExecuteQuery()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return EnumerableExtensions.AsList(connection.ExecuteQuery<TestEnum>("select "));
        }

        [Benchmark]
        public List<TestEnum> Dapper()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return System.Linq.EnumerableExtensions.AsList(connection.Query<string>("select * from dog").Select(i => Enum.Parse<TestEnum>(i)));
        }

        [Benchmark, DapperAot]
        public List<TestEnum> DapperAot()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            return System.Linq.EnumerableExtensions.AsList(connection.Query<string>("select * from dog").Select(i => Enum.Parse<TestEnum>(i)));
        }
    }
}
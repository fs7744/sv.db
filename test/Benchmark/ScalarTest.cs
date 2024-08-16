using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Dapper;
using SV.Db;
using System.Data;

namespace Benchmark
{
    [MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class ScalarTest
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

        public List<string> GetStringListRowCount()
        {
            var dogs = new List<string>(RowCount);
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
        public List<string> ReadEnumerableRowCount()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                return System.Linq.EnumerableExtensions.AsList(cmd.ExecuteReader().ReadEnumerable<string>(RowCount));
            }
            finally
            {
                connection.Close();
            }
        }

        [Benchmark]
        public List<string> ReadEnumerable()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            try
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select ";
                return System.Linq.EnumerableExtensions.AsList(cmd.ExecuteReader().ReadEnumerable<string>());
            }
            finally
            {
                connection.Close();
            }
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
}
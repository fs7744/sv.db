using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using SV.Db;
using System.Text;

namespace Benchmark
{
    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class StringHashingBenchmarks
    {
        [Params(0, 1, 10, 100)]
        public int Count { get; set; }

        public string Str { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var s = string.Join("",Enumerable.Repeat("_", Count));
            var b = Encoding.UTF8.GetBytes(s);
            Random.Shared.NextBytes(b);
            Str = Encoding.UTF8.GetString(b);
        }

        [Benchmark(Baseline = true)]
        public int GetHashCode()
        {
            return Str.GetHashCode();
        }

        [Benchmark]
        public uint SlowNonRandomizedHash()
        {
            return Str.SlowNonRandomizedHash();
        }

        [Benchmark]
        public int NonRandomizedHash()
        {
            return Str.NonRandomizedHash();
        }
    }
}

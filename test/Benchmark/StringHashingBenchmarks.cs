using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using SV;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmark
{
    public static partial class StringHashing
    {
        public static uint SlowNonRandomizedHash(this string? value)
        {
            uint hash = 0;
            if (!string.IsNullOrEmpty(value))
            {
                hash = 2166136261u;
                foreach (char c in value!)
                {
                    hash = (char.ToLowerInvariant(c) ^ hash) * 16777619;
                }
            }
            return hash;
        }
    }

    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class StringHashingBenchmarks
    {
        [Params(0, 1, 10, 100)]
        public int Count { get; set; }

        public string Str { get; set; } = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            var s = string.Join("", Enumerable.Repeat("_", Count));
            var b = Encoding.UTF8.GetBytes(s);
            Random.Shared.NextBytes(b);
            Str = Encoding.UTF8.GetString(b);
        }

        [Benchmark(Baseline = true)]
        public int HashCode()
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
            return Str.HashOrdinalIgnoreCase();
        }

        [Benchmark]
        public uint XxHash32GetBytes()
        {
            return XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(Str));
        }

        [Benchmark]
        public uint XxHash32Span()
        {
            return XxHash32.HashToUInt32(MemoryMarshal.AsBytes(Str.AsSpan()));
        }
    }
}
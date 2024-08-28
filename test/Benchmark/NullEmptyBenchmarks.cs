using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Benchmark
{
    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class NullEmptyBenchmarks
    {
        private readonly List<Customer> data = new();
        private Customer?[] array = [];

        public int Count { get; set; } = 100;

        [GlobalSetup]
        public void Setup()
        {
            data.Clear();
            for (int i = 0; i < Count; i++)
            {
                data.Add(new Customer { Id = i, Name = "Name " + i });
            }
            array = data.ToArray();
        }

        public IEnumerable<Customer> Enumerable()
        {
            foreach (var item in data)
            {
                yield return item;
            }
        }

        [Benchmark(Baseline = true), BenchmarkCategory("List")]
        public bool ListNullEmpty()
        {
            return data == null || data.Count == 0;
        }

        [Benchmark, BenchmarkCategory("List")]
        public bool ListIsNullOrEmpty()
        {
            return data.IsNullOrEmpty();
        }

        [Benchmark, BenchmarkCategory("List")]
        public bool ListIsNotNullOrEmpty()
        {
            return data.IsNotNullOrEmpty();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Array")]
        public bool ArrayNullEmpty()
        {
            return array == null || array.Length == 0;
        }

        [Benchmark, BenchmarkCategory("Array")]
        public bool ArrayIsNullOrEmpty()
        {
            return array.IsNullOrEmpty();
        }

        [Benchmark, BenchmarkCategory("Array")]
        public bool ArrayIsNotNullOrEmpty()
        {
            return array.IsNotNullOrEmpty();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Enumerable")]
        public bool EnumerableNullEmpty()
        {
            return (Enumerable()?.Any()).GetValueOrDefault();
        }

        [Benchmark, BenchmarkCategory("Enumerable")]
        public bool EnumerableIsNullOrEmpty()
        {
            return Enumerable().IsNullOrEmpty();
        }

        [Benchmark, BenchmarkCategory("Enumerable")]
        public bool EnumerableIsNotNullOrEmpty()
        {
            return Enumerable().IsNotNullOrEmpty();
        }

        [Benchmark, BenchmarkCategory("Enumerable")]
        public bool ListEnumerableIsNullOrEmpty()
        {
            return (data as IEnumerable<Customer>).IsNullOrEmpty();
        }

        [Benchmark, BenchmarkCategory("Enumerable")]
        public bool ArrayEnumerableIsNotNullOrEmpty()
        {
            return ((IEnumerable<Customer?>)array).IsNotNullOrEmpty();
        }
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Benchmark
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class ChunckBenchmarks
    {
        private readonly List<Customer> data = new();
        private Customer[] array = [];

        [Params(0, 1, 10, 100, 1000, 10000, 100000)]
        public int Count { get; set; }

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

        [Benchmark(Baseline = true)]
        public int SumForeach()
        {
            int sum = 0;
            foreach (var customer in data)
            {
                sum += customer.Id;
            }
            return sum;
        }

        [Benchmark]
        public int ListChunk()
        {
            int sum = 0;
            foreach (var customer in data.Page(100))
            {
                foreach (var item in customer)
                {
                    sum += item.Id;
                }
            }
            return sum;
        }

        [Benchmark]
        public int ArrayChunk()
        {
            int sum = 0;
            foreach (var customer in array.Page(100))
            {
                foreach (var item in customer)
                {
                    sum += item.Id;
                }
            }
            return sum;
        }

        [Benchmark]
        public int EnumerableChunk()
        {
            int sum = 0;
            foreach (var customer in Enumerable().Page(100))
            {
                foreach (var item in customer)
                {
                    sum += item.Id;
                }
            }
            return sum;
        }
    }
}
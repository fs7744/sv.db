using BenchmarkDotNet.Attributes;
using SV.Db;

namespace Benchmark
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    [ShortRunJob, MemoryDiagnoser]
    public class ChunckBenchmarks
    {
        private readonly List<Customer> data = new();

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
        }

        [Benchmark(Baseline = true)]
        public int Sum()
        {
            int sum = 0;
            foreach (var customer in data)
            {
                sum += customer.Id;
            }
            return sum;
        }

        [Benchmark]
        public int Chunk()
        {
            int sum = 0;
            foreach (var customer in data.Chunk(100))
            {
                foreach (var item in customer)
                {
                    sum += item.Id;
                }
            }
            return sum;
        }

        [Benchmark]
        public int Page()
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
    }
}

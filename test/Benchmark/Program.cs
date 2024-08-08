using Benchmark;
using BenchmarkDotNet.Running;
var a = new ChunckBenchmarks() { Count = 955 };
a.Setup();
var c = a.ChunkEnumerable();
var d = a.PageEnumerable();
var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

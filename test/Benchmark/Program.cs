using Benchmark;
using BenchmarkDotNet.Running;
var a = new ChunckBenchmarks() { Count = 955 };
a.Setup();
var c = a.Chunk();
var d = a.Page();
var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

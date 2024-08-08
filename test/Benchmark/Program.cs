using Benchmark;
using BenchmarkDotNet.Running;
var a = new ChunckBenchmarks() { Count = 955 };
a.Setup();
var c = a.ArrayChunk();
var c2 = a.ListChunk();
var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

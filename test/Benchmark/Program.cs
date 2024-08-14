using Benchmark;
using BenchmarkDotNet.Running;
using System.IO.Hashing;
using System.Text;

var a = new StringHashingBenchmarks();
a.Setup();
var d = a.XxHash32GetBytes();
var d1 = a.XxHash32Span();
var summary = BenchmarkRunner.Run<StringHashingBenchmarks>();
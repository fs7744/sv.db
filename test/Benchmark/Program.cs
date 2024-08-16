using Benchmark;
using BenchmarkDotNet.Running;

var a = new ScalarTest() { RowCount = 12 };
var d0 = a.GetStringList();
var d1 = a.ReadEnumerable();
var d2 = a.Dapper();
var d3 = a.DapperAot();
var summary = BenchmarkRunner.Run<ScalarTest>();
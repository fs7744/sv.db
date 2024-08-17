using Benchmark;
using BenchmarkDotNet.Running;

var a = new ScalarOneTest() { RowCount = 12 };
var d0 = a.GetString();
var d1 = a.ExecuteScalar();
var d2 = a.Dapper();
var d3 = a.DapperAot();
var summary = BenchmarkRunner.Run<ScalarOneTest>();
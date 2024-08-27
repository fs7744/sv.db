using Benchmark;
using BenchmarkDotNet.Running;

//var a = new ScalarOneTest() { RowCount = 12 };
//var d0 = a.GetString();
//var d1 = a.ExecuteScalar();
//var d2 = a.Dapper();
//var d3 = a.DapperAot();
//var d4 = a.ExecuteQueryFirstOrDefault();
//var summary = BenchmarkRunner.Run<ScalarOneTest>();

//var a = new ScalarListTest() { RowCount = 12 };
//var d0 = a.GetStringList();
//var d1 = a.ExecuteQuery();
//var d2 = a.Dapper();
//var d3 = a.DapperAot();
//var d4 = a.ExecuteQueryRowCount();

//var summary = BenchmarkRunner.Run<ScalarListTest>();

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
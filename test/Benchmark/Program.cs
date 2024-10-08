﻿using Benchmark;
using BenchmarkDotNet.Running;

//var a = new ScalarOneTest() { RowCount = 12 };
//var d0 = a.GetString();
//var d1 = a.ExecuteScalar();
//var d2 = a.Dapper();
//var d3 = a.DapperAot();
//var d4 = a.ExecuteQueryFirstOrDefault();
//var summary = BenchmarkRunner.Run<ScalarOneTest>();

var a = new SetParamsBenchmarks() { RowCount = 12 };
a.SetParams();
a.SetParamsClass();
a.SetParamsStruct();
a.SetParamsTuple();
a.SetParamsAnonymous();
a.SetParamsRecordStruct();
a.SetParamsRecordClass();

var summary = BenchmarkRunner.Run<SetParamsBenchmarks>();

//var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
﻿using Benchmark;
using BenchmarkDotNet.Running;

var a = new StringHashingBenchmarks();
a.Setup();
var d = a.XxHash32GetBytes();
var d1 = a.XxHash32Span();
var summary = BenchmarkRunner.Run<StringHashingBenchmarks>();
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using SV.Db;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [ShortRunJob, MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
    public class SetParamsBenchmarks
    {
        public int RowCount { get; set; } = 1;

        private TestData data = new TestData(("a", "oo"));

        private ClassTestData classTestData = new ClassTestData() { Decimal =3, Decimal2 = 4, Id =5, Id2 = 6, Single = 8, Single2 =77 };

        private StructTestData2 structTestData2 = new StructTestData2() { Decimal = 3, Decimal2 = 4, Id = 5, Id2 = 6, Single = 8, Single2 = 77 };

        private (Decimal Decimal, Decimal Decimal2, int Id, int Id2, Single Single, Single Single2) tuple = ( Decimal : 3, Decimal2 : 4, Id : 5, Id2 : 6, Single : 8, Single2 : 77 );

        [Benchmark(Baseline = true)]
        public void SetParams()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            var cmd = connection.CreateCommand();
            var ps = cmd.Parameters;
            var args = classTestData;
            DbParameter p;
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Id";
            p.DbType = DbType.Int32;
            p.Value = args.Id;
            ps.Add(p);
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Decimal";
            p.DbType = DbType.Decimal;
            p.Value = args.Decimal;
            ps.Add(p);
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Single";
            p.DbType = DbType.Single;
            p.Value = args.Single;
            ps.Add(p);
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Id2";
            p.DbType = DbType.Int32;
            p.Value = args.Id2;
            ps.Add(p);
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Decimal2";
            p.DbType = DbType.Decimal;
            p.Value = args.Decimal2;
            ps.Add(p);
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = "Single2";
            p.DbType = DbType.Single;
            p.Value = args.Single2;
            ps.Add(p);
        }

        [Benchmark]
        public void SetParamsClass()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            connection.CreateCommand().SetParams(classTestData);
        }

        [Benchmark]
        public void SetParamsStruct()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            connection.CreateCommand().SetParams(structTestData2);
        }

        [Benchmark]
        public void SetParamsTuple()
        {
            var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
            connection.CreateCommand().SetParams(tuple);
        }

        //[Benchmark]
        //public void SetParamsAnonymous()
        //{
        //    var connection = new TestDbConnection() { RowCount = RowCount, Data = data };
        //    connection.CreateCommand().SetParams(new  { Decimal = 3, Decimal2 = 4, Id = 5, Id2 = 6, Single = 8, Single2 = 77 });
        //}
    }

    public class ClassTestData
    {
        public int Id { get; set; }
        public Decimal Decimal { get; set; }
        public Single Single { get; set; }

        public int Id2;
        public Decimal Decimal2;
        public Single Single2;
    }

    public struct StructTestData2
    {
        public int Id { get; set; }
        public Decimal Decimal { get; set; }
        public Single Single { get; set; }

        public int Id2;
        public Decimal Decimal2;
        public Single Single2;
    }
}

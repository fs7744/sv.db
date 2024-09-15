using SV.Db;
using System.Data.Common;
using System.Data;

namespace UT.GeneratorTestCases
{
    public class TestDd
    {
        public int Int32 { get; set; }
    }

    public class Te : RecordFactory<dynamic>
    {
        public static readonly RecordFactory<dynamic> Instance = new Te();

        public override void SetParams(IDbCmd cmd, dynamic args)
        {
            var ps = cmd.Parameters;
            DbParameter p;

            p = cmd.CreateParameter();
            p.ParameterName = "Int32";
            p.Value = args.Int32;
            p.DbType = DbType.Int32;
            ps.Add(p);
        }

        protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
        {
        }

        protected override dynamic? Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            return null;
        }
    }
}
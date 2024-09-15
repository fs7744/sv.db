using SV.Db;
using System.Data.Common;
using System.Data;
using System.Runtime.CompilerServices;

namespace UT.GeneratorTestCases
{
    public class TestDd
    {
        public int Int32 { get; set; }
    }

    public class Te : RecordFactory<(dynamic f, int)>
    {
        public static readonly RecordFactory<(dynamic f, int)> Instance = new Te();

        public override void SetParams(IDbCmd cmd, (dynamic f, int) args)
        {
            var ps = cmd.Parameters;
            DbParameter p;

            p = cmd.CreateParameter();
            p.ParameterName = "Int32";
            p.Value = args.f;
            p.DbType = DbType.Int32;
            ps.Add(p);
        }

        protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
        {
        }

        protected override (dynamic f, int) Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            return (default, default);
        }
    }
}
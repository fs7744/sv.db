using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    public class TestDd
    {
        public int Int32 { get; set; }
    }

    public class Te : RecordFactory<TestDd>
    {
        public override void SetParams(IDbCmd cmd, TestDd args)
        {
            throw new NotImplementedException();
        }

        protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
        {
            throw new NotImplementedException();
        }

        protected override TestDd? Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            throw new NotImplementedException();
        }
    }
}
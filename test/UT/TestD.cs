using SV.Db;
using System.Data.Common;
using System.Data;
using SV;
using System.Runtime.CompilerServices;

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
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var type = reader.GetFieldType(i);
                switch (StringHashing.HashOrdinalIgnoreCase(name))
                {
                    case 1369956181:
                        tokens[i] = type == typeof(int) ? 1 : 2;
                        break;

                    default:
                        break;
                }
            }
        }

        protected override TestDd? Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            var d = new TestDd();
            for (int j = 0; j < tokens.Length; j++)
            {
                switch (tokens[j])
                {
                    case 1:
                        d.Int32 = reader.GetInt32(j);
                        break;

                    case 2:
                        d.Int32 = DBUtils.As<int>(reader.GetValue(j));
                        break;

                    default:
                        break;
                }
            }
            return d;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SV.Db;
using Xunit.Abstractions;

namespace UT
{
    public class ParamTest : GeneratorTestBase
    {
        public ParamTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void GenerateTest()
        {
            (var compilation, var result) = TestGenerate(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SV.Db;
using System.Data.Common;

namespace MyCode
{
    public class TestD
    {
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            DbConnection connection = null;
            connection.ExecuteQueryFirstOrDefault<string>(""select * from dog"", new TestD());
            connection.CreateCommand().SetParams<string>(string.Empty);
            connection.ExecuteNonQueryAsync("""", (2, Name: 3));
            var b = (4, Name: 3);
            connection.ExecuteNonQueryAsync("""", b);
            object c = (4, Name: 3);
            connection.ExecuteNonQueryAsync("""", args: c);

            connection.ExecuteNonQueryAsync("""", """");
        }
    }
}
");
        }
    }
}
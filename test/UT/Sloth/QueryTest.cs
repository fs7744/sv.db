using SV.Db.Sloth;
using SV.Db.Sloth.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT.Sloth
{
    [Table("QueryTest")]
    public class QueryTest
    {
        public int A { get; set; }
        public int B { get; set; }

        [Fact]
        public void SelectFields()
        {
            var a = Query.From<QueryTest>();
            a.Select(nameof(QueryTest.A), nameof(QueryTest.B));
            a.Select(i => i.A, i => i.B);
        }

        [Fact]
        public void Where()
        {
            var a = Query.From<QueryTest>();
            a.Where(i => i.A == 1 || i.B == 2 || i.B.Like(""));
        }
    }
}
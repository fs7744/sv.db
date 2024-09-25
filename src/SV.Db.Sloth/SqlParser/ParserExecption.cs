using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV.Db.Sloth.SqlParser
{
    public class ParserExecption : Exception
    {
        public ParserExecption(string? message) : base(message)
        {
        }
    }
}